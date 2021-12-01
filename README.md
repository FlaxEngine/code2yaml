# code2yaml for Flax
A tool to extract metadata from code and export as yaml files for [Flax Docs](https://docs.flaxengine.com/).

## configure code2yaml
To use the tool, you need to provide a config file `code2yaml.json`.

Here is a simple `code2yaml.json`.

```json
{
  "input_paths": ["./my-project"],
  "output_path": "./output",
  "language": "cpp"
}
```

* `input_paths`: an array of input paths.
* `output_path`: output path
* `exclude_paths`: an array of exclude paths. Code in the paths wouldn't be extracted metadata.
* `language`: it now supports `cplusplus`, `java`.
* `repo_remap`: remaps the repository urls
* `exclude_types`: excluides types by matching regex
* `doxygen_template_file`: custom Doxygen file template
* `assembly`: override assembly name entry

> *Note*
> all the paths(path in `input_paths`, `exclude_paths` or `output_path`) are either absolute path or path relative to code2yaml.json

## run code2yaml
1. build the solution.
   open cmd shell. `build.cmd`
2. `code2yaml.exe code2yaml.json`
