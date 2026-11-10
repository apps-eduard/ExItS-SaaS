# Architecture

```
src/ExItS.Premium/ExItS.Premium.Web
  src/styles        tokens and the document reset
  src/theme         appearance state and storage
  src/icons         ExIcon, registry, self-hosted font
  src/motion        shared spring
  src/components    one folder per component
  src/playground    design-system application
  docs              standards for future UI work
```

The playground is the only route. Styling stays in modules and tokens. Do not import V1 CSS.

Preferences are local to this app. There is no backend client in this phase.

V1 remains on ports 5177 and 5178. This app uses 5180.
