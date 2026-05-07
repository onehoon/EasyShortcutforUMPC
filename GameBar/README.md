GameBar web assets note

- This project currently renders the widget UI from XAML (`MainPage.xaml`), not from HTML.
- `GameBar/Widget.html` is kept only as a fallback/diagnostic asset.
- `GameBar/index.html` was removed to avoid duplicate maintenance drift.
- If the project returns to a WebView-based widget flow, use a single HTML entry file to avoid duplication.
