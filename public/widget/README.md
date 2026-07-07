# Web chat widget embed

Add this snippet before `</body>` on any site to show the floating chat launcher:

```html
<script
  src="https://flow-scribe-71.lovable.app/widget/loader.js"
  data-api-base="https://contact-center.alkhwarizmi.pro"
  data-project-id="YOUR_PROJECT_ID"
  data-chatbot-id="YOUR_CHATBOT_ID"
  data-agent-channel="webwidget"
  data-title="Chat with us"
  data-subtitle="We reply in a few minutes"
  data-accent="#2563eb"
  data-departments='[{"id":"sales","name":"Sales"},{"id":"support","name":"Support"}]'
  data-lang="en"
  defer
></script>
```

The loader injects an iframe pointing at `/widget?mode=embed&...` in this app, which renders the same React `<WebChatWidget />` and connects to `CustomerHub` (`/hubs/customer`) via SignalR.

To test in-app, open `/widget?apiBase=...&projectId=...&chatbotId=...` directly.
