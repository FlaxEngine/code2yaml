namespace Microsoft.Content.Build.Code2Yaml.Steps
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Microsoft.Content.Build.Code2Yaml.ArticleGenerator;
    using Microsoft.Content.Build.Code2Yaml.Common;
    using Microsoft.Content.Build.Code2Yaml.Constants;
    using Microsoft.Content.Build.Code2Yaml.DataContracts;
    using Microsoft.Content.Build.Code2Yaml.Utility;

    using DocAsCode.YamlSerialization;

    public class GenerateArticles : IStep
    {
        private static readonly ThreadLocal<YamlSerializer> YamlSerializer = new ThreadLocal<YamlSerializer>(() =>
        {
            return new YamlSerializer();
        });

        public string StepName
        {
            get { return "GenerateArticles"; }
        }

        public IArticleGenerator Generator { get; set; }

        public async Task RunAsync(BuildContext context)
        {
            var config = context.GetSharedObject(Constants.Config) as ConfigModel;
            if (config == null)
            {
                throw new ApplicationException(string.Format("Key: {0} doesn't exist in build context", Constants.Config));
            }
            string inputPath = StepUtility.GetProcessedXmlOutputPath(config.OutputPath);
            string outputPath = config.OutputPath;
            var changesDict = context.GetSharedObject(Constants.Changes) as Dictionary<string, HierarchyChange>;
            if (changesDict == null)
            {
                throw new ApplicationException(string.Format("Key: {0} doesn't exist in build context", Constants.Changes));
            }

            var infoDict = new ConcurrentDictionary<string, ArticleItemYaml>();
            context.SetSharedObject(Constants.ArticleItemYamlDict, infoDict);
            var pages = new ConcurrentBag<PageModel>();
            await changesDict.Values.ForEachInParallelAsync(
                async change =>
                {
                    var path = Path.Combine(inputPath, PathUtility.FilterPath(change.File));
                    IArticleGenerator generator = (IArticleGenerator)Generator.Clone();
                    var cloned = context.Clone();
                    cloned.SetSharedObject(Constants.CurrentChange, change);
                    HierarchyChange parent = change.Parent != null ? changesDict[change.Parent] : null;
                    cloned.SetSharedObject(Constants.ParentChange, parent);
                    switch (change.Type)
                    {
                    case HierarchyType.Namespace:
                    case HierarchyType.Class:
                    case HierarchyType.Struct:
                    case HierarchyType.Interface:
                    case HierarchyType.Enum:
                        if (!File.Exists(path))
                            return;
                        using (var input = File.OpenRead(path))
                        {
                            XDocument doc = XDocument.Load(input);
                            PageModel page = await generator.GenerateArticleAsync(cloned, doc);
                            if (page != null)
                            {
                                AddPage(infoDict, page, change.File);
                                pages.Add(page);
                            }
                        }
                        break;
                    case HierarchyType.File:
                        path = Path.Combine(inputPath, Constants.IndexFileName);
                        using (var stream = File.OpenRead(path))
                        {
                            XDocument doc = XDocument.Load(stream);
                            var compound = doc.Root.Elements("compound").First(x => (string)x.Attribute("refid") == change.Uid);
                            var fileName = compound.Element("name").Value;
                            var members = compound.Elements("member");
                            var enumMembers = members.Where(x => (string)x.Attribute("kind") == "enum");
                            var enumvalueMembers = members.Where(x => (string)x.Attribute("kind") == "enumvalue");
                            var dirMembers = doc.Root.Elements("compound").Where(x => (string)x.Attribute("kind") == "dir");
                            var dirs = dirMembers.Select(x => (string)x.Element("name").Value);
                            string filePath = null;
                            foreach (var dir in dirs)
                            {
                                filePath = Path.Combine(dir, fileName);
                                if (File.Exists(filePath))
                                    break;
                                filePath = null;
                            }
                            foreach (var enumMember in enumMembers)
                            {
                                var name = (string)enumMember.Element("name").Value;
                                var refid = (string)enumMember.Attribute("refid");

                                // Generate xml doc for enum article generation (doxygen doesn't emit enums properly to the xml output)
                                XDocument enumDoc = new XDocument();
                                var doxygen = new XElement("doxygen");
                                enumDoc.Add(doxygen);
                                var compounddef = new XElement("compounddef");
                                doxygen.Add(compounddef);
                                compounddef.SetAttributeValue("id", name);
                                compounddef.SetAttributeValue("kind", "enum");
                                compounddef.SetAttributeValue("language", "C++");
                                compounddef.SetAttributeValue("prot", "public");
                                var compoundname = new XElement("compoundname");
                                compoundname.SetValue(name);
                                compounddef.Add(compoundname);
                                if (filePath != null)
                                {
                                    var location = new XElement("location");
                                    location.SetAttributeValue("file", filePath);
                                    compounddef.Add(location);
                                }
                                var sectiondef = new XElement("sectiondef");
                                sectiondef.SetAttributeValue("kind", "public-attrib");
                                compounddef.Add(sectiondef);
                                var valuesMembers = enumvalueMembers.Where(x => ((string)x.Attribute("refid")).StartsWith(refid));
                                foreach (var valueMember in valuesMembers)
                                {
                                    var valueName = (string)valueMember.Element("name").Value;
                                    var valueId = (string)valueMember.Attribute("refid");

                                    var memberdef = new XElement("memberdef");
                                    memberdef.SetAttributeValue("kind", "variable");
                                    memberdef.SetAttributeValue("id", name + '.' + valueName);
                                    memberdef.SetAttributeValue("prot", "public");
                                    memberdef.SetAttributeValue("static", "no");
                                    memberdef.SetAttributeValue("mutable", "no");
                                    sectiondef.Add(memberdef);
                                    
                                    memberdef.Add(new XElement("type"){
                                        Value = name,
                                    });
                                    memberdef.Add(new XElement("name"){
                                        Value = valueName,
                                    });
                                    memberdef.Add(new XElement("definition"){
                                        Value = name + " " + name + "::" + valueName,
                                    });
                                }

                                //Console.WriteLine("====");
                                //Console.WriteLine(doxygen.ToString());
                                //Console.WriteLine("====");

                                var enumChange = new HierarchyChange
                                {
                                    Uid = name,
                                    Name = name,
                                    File = name + Constants.YamlExtension,
                                    Type = HierarchyType.Enum,
                                    Parent = null,
                                    Children = new HashSet<string>(),
                                };
                                cloned.SetSharedObject(Constants.CurrentChange, enumChange);
                                cloned.SetSharedObject(Constants.ParentChange, null);
                                PageModel page = await generator.GenerateArticleAsync(cloned, enumDoc);
                                if (page != null)
                                {
                                    AddPage(infoDict, page, change.File);
                                    pages.Add(page);
                                }
                            }
                        }
                        break;
                    }
                });

            // Organize types into categories based on the module name from the source file path (approx)
            var modulePages = new Dictionary<string, PageModel>();
            foreach (var page in pages)
            {
                if (page == null || page.Items == null)
                    continue;
                foreach (var item in page.Items)
                {
                    if (item.Parent != null)
                        continue;
                    var mainItem = item;
                    if (mainItem.FilePath == null)
                    {
                        // Namespaces might not have any specific file set so deduct it from the children
                        mainItem = null;
                        foreach (var child in item.Children)
                        {
                            ArticleItemYaml childItem = null;
                            foreach (var e in pages)
                            {
                                foreach (var q in e.Items)
                                {
                                    if (q.Uid == child)
                                    {
                                        childItem = q;
                                        break;
                                    }
                                }
                                if (childItem != null)
                                    break;
                            }
                            if (childItem != null && childItem.FilePath != null)
                            {
                                mainItem = childItem;
                                break;
                            }
                        }
                        if (mainItem == null || mainItem.FilePath == null)
                            continue;
                    }

                    string moduleName = null;
                    if (mainItem.FilePath.StartsWith("Engine/"))
                        moduleName = mainItem.FilePath.Substring(7, mainItem.FilePath.IndexOf('/', 7) - 7);
                    else if (mainItem.FilePath.StartsWith("Editor/"))
                        moduleName = "Editor";
                    else if (mainItem.FilePath.StartsWith("ThirdParty/"))
                        moduleName = "ThirdParty";
                    if (moduleName == null)
                        continue;

                    if (!modulePages.TryGetValue(moduleName, out var modulePage))
                    {
                        var moduleId = "Module." + moduleName;
                        var moduleDisplayName = moduleName + " Module";
                        var mmoduleItem = new ArticleItemYaml
                        {
                            Uid = moduleId,
                            Id = moduleId,
                            Parent = null,
                            Children = new List<string>(),
                            Href = moduleId + Constants.YamlExtension,
                            Name = moduleDisplayName,
                            NameWithType = moduleDisplayName,
                            FullName = moduleDisplayName,
                            Type = MemberType.Namespace,
                            SupportedLanguages = mainItem.SupportedLanguages,
                            AssemblyNameList = mainItem.AssemblyNameList,
                        };
                        modulePage = new PageModel
                        {
                            Items = new List<ArticleItemYaml> { mmoduleItem },
                            References = new List<ReferenceViewModel>(),
                        };
                        AddPage(infoDict, modulePage, config.OutputPath);
                        modulePages.Add(moduleName, modulePage);
                    }

                    var moduleItem = modulePage.Items[0];
                    item.Parent = moduleItem.Uid;
                    moduleItem.Children.Add(item.Uid);
                    modulePage.References.Add(new ReferenceViewModel { Uid = item.Uid, });
                    page.References.Add(new ReferenceViewModel { Uid = moduleItem.Uid, });
                }
            }
            foreach (var e in modulePages)
                pages.Add(e.Value);

            // update type declaration/reference and save yaml
            await pages.ForEachInParallelAsync(
                async page =>
                {
                    if (page == null)
                        return;
                    if (page.Items == null || page.Items.Count == 0)
                        throw new Exception("Invalid page Items");
                    var href = page.Items[0].Href;
                    if (string.IsNullOrEmpty(href))
                        throw new Exception("Invalid Href on page " + page.Items[0].Name);

                    // update declaration
                    var cloned = context.Clone();
                    await Generator.PostGenerateArticleAsync(cloned, page);

                    // update reference
                    foreach (var reference in page.References)
                    {
                        ArticleItemYaml yaml;
                        if (infoDict.TryGetValue(reference.Uid, out yaml))
                        {
                            reference.CommentId = yaml.CommentId;
                            reference.Name = yaml.Name;
                            reference.NameWithType = yaml.NameWithType;
                            reference.FullName = yaml.FullName;
                            reference.Href = yaml.Href;
                            reference.Parent = yaml.Parent;
                            reference.IsExternal = true;
                        }
                    }
                    using (var writer = new StreamWriter(Path.Combine(outputPath, href)))
                    {
                        writer.WriteLine(Constants.YamlMime.ManagedReference);
                        YamlSerializer.Value.Serialize(writer, page);
                    }
                });

            context.SetSharedObject(Constants.Pages, pages);
        }

        private void AddPage(ConcurrentDictionary<string, ArticleItemYaml> infoDict, PageModel page, string file)
        {
            foreach (var item in page.Items)
            {
                if (!infoDict.TryAdd(item.Uid, item))
                {
                    ConsoleLogger.WriteLine(
                        new LogEntry
                        {
                            Phase = StepName,
                            Level = LogLevel.Warning,
                            Message = $"Duplicate items {item.Uid} found in {file}.",
                        });
                }
            }
        }
    }
}
