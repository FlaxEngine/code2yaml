namespace Microsoft.Content.Build.Code2Yaml.Steps
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Content.Build.Code2Yaml.Common;
    using Microsoft.Content.Build.Code2Yaml.Constants;
    using Microsoft.Content.Build.Code2Yaml.DataContracts;
    using Microsoft.Content.Build.Code2Yaml.NameGenerator;
    using Microsoft.Content.Build.Code2Yaml.Utility;

    using DocAsCode.YamlSerialization;

    public class GenerateToc : IStep
    {
        public INameGenerator NameGenerator { get; set; }

        public string StepName
        {
            get { return "GenerateToc"; }
        }

        public Task RunAsync(BuildContext context)
        {
            var config = context.GetSharedObject(Constants.Config) as ConfigModel;
            if (config == null)
            {
                throw new ApplicationException(string.Format("Key: {0} doesn't exist in build context", Constants.Config));
            }
            string outputPath = config.OutputPath;
            var changesDict = context.GetSharedObject(Constants.Changes) as Dictionary<string, HierarchyChange>;
            if (changesDict == null)
            {
                throw new ApplicationException(string.Format("Key: {0} doesn't exist in build context", Constants.Changes));
            }
            var pages = context.GetSharedObject(Constants.Pages) as IEnumerable<PageModel>;
            if (pages == null)
            {
                throw new ApplicationException(string.Format("Key: {0} doesn't exist in build context", Constants.Pages));
            }
            /*TocYaml tocYaml = new TocYaml(
                from change in changesDict.Values
                where change.Parent == null && !YamlUtility.SkipTypeFilter(config, change.Name) && change.Type != HierarchyType.File
                let toc = FromHierarchyChange(changesDict, change, context)
                orderby toc.Name.ToLower()
                select toc);
            foreach (var page in pages)
            {
                if (page == null || page.Items == null)
                    continue;
                foreach (var item in page.Items)
                {
                    if (!ContainsHref(tocYaml, item.Href))
                    {
                        tocYaml.Add(FromItem(changesDict, item, context));
                    }
                }
            }*/

            var tocYaml = new TocYaml();
            var idToToc = new Dictionary<string, TocItemYaml>();
            var idToItem = new Dictionary<string, ArticleItemYaml>();
            foreach (var page in pages)
            {
                if (page == null || page.Items == null)
                    continue;
                foreach (var item in page.Items)
                {
                    var tocItem = GetTocItem(pages, idToToc, idToItem, item);
                    if (string.IsNullOrEmpty(item.Parent))
                        tocYaml.Add(tocItem);
                }
            }
            SortToc(tocYaml);

            string tocFile = Path.Combine(outputPath, Constants.TocYamlFileName);
            using (var writer = new StreamWriter(tocFile))
            {
                writer.WriteLine(Constants.YamlMime.TableOfContent);
                new YamlSerializer().Serialize(writer, tocYaml);
            }

            bool generateTocMDFile = config.GenerateTocMDFile;
            if (generateTocMDFile)
            {
                string tocMDFile = Path.Combine(outputPath, Constants.TocMDFileName);
                using (var writer = new StreamWriter(tocMDFile))
                {
                    foreach (var item in tocYaml)
                    {
                        WriteTocItemMD(writer, item, 1);
                    }
                }
            }

            return Task.FromResult(1);
        }

        private TocItemYaml FromHierarchyChange(IReadOnlyDictionary<string, HierarchyChange> changeDict, HierarchyChange change, BuildContext context)
        {
            var parentChange = change.Parent != null ? changeDict[change.Parent] : null;
            var children = change.Children ?? new HashSet<string>();
            return new TocItemYaml
            {
                Uid = change.Uid,
                Name = NameGenerator.GenerateTypeName(new NameGeneratorContext { CurrentChange = change, ParentChange = parentChange }, null, true),
                Href = YamlUtility.ParseHrefFromChangeFile(PathUtility.FilterPath(change.File)),
                Items = children.Any() ? new TocYaml(
                from child in children
                let toc = FromHierarchyChange(changeDict, changeDict[child], context)
                orderby toc.Name.ToLower()
                select toc) : null,
            };
        }

        private TocItemYaml FromItem(IReadOnlyDictionary<string, HierarchyChange> changeDict, ArticleItemYaml item, BuildContext context)
        {
            return new TocItemYaml
            {
                Uid = item.Uid,
                Name = item.Name,
                Href = item.Href,
            };
        }

        private TocItemYaml GetTocItem(IEnumerable<PageModel> pages, Dictionary<string, TocItemYaml> idToToc, Dictionary<string, ArticleItemYaml> idToItem, ArticleItemYaml item)
        {
            if (!idToToc.TryGetValue(item.Id, out var tocItem))
            {
                tocItem = new TocItemYaml
                {
                    Uid = item.Uid,
                    Name = item.Name,
                    Href = item.Href,
                };
                if (item.Children != null && item.Children.Count != 0)
                {
                    tocItem.Items = new TocYaml();
                    foreach (var child in item.Children)
                    {
                        if (!idToToc.TryGetValue(child, out var childTocItem))
                        {
                            if (!idToItem.TryGetValue(child, out var childItem))
                            {
                                foreach (var page in pages)
                                {
                                    childItem = page.Items.FirstOrDefault(x => x.Uid == child);
                                    if (childItem != null)
                                        break;
                                }
                                if (childItem == null || !FilterItem(childItem))
                                    continue;
                                idToItem.Add(child, childItem);
                            }
                            childTocItem = GetTocItem(pages, idToToc, idToItem, childItem);
                        }
                        tocItem.Items.Add(childTocItem);
                    }
                }
                idToToc.Add(item.Id, tocItem);
            }
            return tocItem;
        }

        private static void SortToc(TocYaml toc)
        {
            if (toc == null || toc.Count == 0)
                return;
            toc.Sort((x, y) => x.Name.CompareTo(y.Name));
            foreach (var e in toc)
                SortToc(e.Items);
        }

        private static bool FilterItem(ArticleItemYaml item)
        {
            switch (item.Type)
            {
            case MemberType.Assembly:
            case MemberType.Namespace:
            case MemberType.Class:
            case MemberType.Enum:
            case MemberType.Struct:
            case MemberType.Interface:
                return true;
            }
            return false;
        }

        private static bool ContainsHref(TocYaml yaml, string href)
        {
            return yaml.Any(x => ContainsHref(x, href));
        }

        private static bool ContainsHref(TocItemYaml itemYaml, string href)
        {
            return itemYaml.Href == href || (itemYaml.Items != null && itemYaml.Items.Any(x => ContainsHref(x, href)));
        }

        private void WriteTocItemMD(StreamWriter writer, TocItemYaml item, int depth)
        {
            writer.WriteLine($"{new string('#', depth)} [{item.Name}]({item.Href})");
            if (item.Items != null)
            {
                foreach (var c in item.Items)
                {
                    WriteTocItemMD(writer, c, depth + 1);
                }
            }
        }
    }
}
