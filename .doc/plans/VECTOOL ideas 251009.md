# VECTOOL BUGS, Enhancement Ideas

## Idea prio 1 
- store per project configs in project root and read from there when necessary.

## BUG Editing configured folder Exclusions - WIP
BUG Editing configured folder Exclusions is ineffective by the editor on the settings tab. Edits are not stored and not effective in the app.

## Idea prio 2 
- store parametrized prompts and copy to clipboard
- actually let's have a full fledged prompt handler app integrated

## Git Submodule changed files are not included in owner git repo changes

``` markdown
  - ### Submodules

- LogCtx
  - Changes saved to: VecToolDev.feat_4.0.p3_LogCtx_Fluent_API.changes-vectoolDev-LogCtx-git-changes.md
```

## a vectorstore is a collection of documents:

- a root folder with git, *.sln, etc
- several other project part folders for creating separated docs
- root document could exclude/include project part folders

## get git repository comments for a time interval

- should be able to add (dragndrop) other relevant documents for a VStore (plans, documentation, etc)
- Recent files should be filtered by current VStore, if necessary, and sorted by columns

- git log <branch>
- dotnet test --filter "FullyQualifiedName~VstpBuilderTests"
- other console commands, resulting a txt file and appearing in the generated documents
