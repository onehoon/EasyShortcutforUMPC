Store technical note: helper execution vs input delivery

Current behavior (kept as-is for this release)

- Product purpose:
  - This is a productivity widget for Windows handheld/UMPC-class devices that may not have a physical keyboard.
  - It helps users trigger keyboard-dependent tool shortcuts more efficiently from Xbox Game Bar.
  - It is not intended as a generic macro tool; it is a focused productivity helper for specific overlay/scaling workflows.

Primary shortcut intents

- `OptiScaler Overlay` (`Alt+Insert`):
  - Opens the overlay shortcut path used by the OptiScaler upscaling/frame-generation workflow.
- `Lossless Scaling` (`Ctrl+Alt+S`):
  - Triggers the shortcut used to launch/use the Lossless Scaling upscaling/frame-generation workflow.

- Main app calls `FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(...)`.
- The app can confirm helper launch request success/failure.
- The close action uses an explicit confirmation dialog before sending Alt+F4 (no single-tap force action).
- The app cannot directly confirm whether the target key input was actually accepted by the foreground app/game.
- The helper process is not resident in memory: it is launched only when a user presses a widget button, performs a single requested action, and exits immediately.

Full trust capability scope

- The app declares `runFullTrust` only to launch its packaged helper component (`ShortcutHelper.exe`) for user-requested shortcut delivery.
- `ShortcutHelper.exe` is bundled inside the app package and is not downloaded from external sources.
- Full trust usage is strictly on-demand:
  - launched from an explicit widget button press
  - performs one requested shortcut action
  - exits immediately
- No persistent background helper/service is used.
- No keylogging, no capture of user text input, no remote control, no game memory manipulation, and no process injection are performed.
- Full trust is not used for hidden automation loops; debounce/focus-settle delays are reliability guards to avoid accidental duplicate input.
- This is an accepted limitation for the current release scope.

Why this is acceptable for now

- Release UI intentionally avoids verbose debug/state output.
- Debug-only logging remains available for investigation.
- Helper logs are constrained (debug-only, size-limited) to reduce persistent local footprint.
- Foreground process name/PID logging is debug-only and not present in release distribution builds.
- Release builds do not collect or transmit per-app usage/process telemetry.

Operational interpretation

- "Helper launch success" != "input delivery success"
- Typical delivery blockers can include:
  - focus not returning to target app in time
  - target app input policy/UIPI restrictions
  - overlay interaction timing

Recommended user-facing behavior (release)

- Keep user messages short and generic.
- Do not expose internal exception types, local paths, command ids, or process details in release UI.

Input safety and anti-abuse scope

- A short debounce guard and a brief post-overlay focus settle delay are used only to improve reliability and prevent accidental duplicate input.
- This is not macro/cheat automation behavior:
  - no background loop
  - no unattended scheduling
  - no game hooking or memory manipulation
  - no remote control functionality
- Users cannot arbitrarily reprogram shortcut behavior inside the widget (no custom key remap UI, no user-defined macro creation, no repeat-loop editor).
- Shortcut dispatch is intentionally guarded (debounce and focus-settle delays), so it is not designed for ultra-fast repeated triggering.
- The flow closes/dismisses Game Bar context before final key dispatch to prioritize correct foreground delivery instead of rapid-fire execution.

Post-store roadmap (proposal only; not implemented)

1) Add helper result feedback channel
- Option A: AppService response
- Option B: short-lived result token file + bounded polling
- Option C: named pipe (higher complexity)

2) Define explicit result codes
- `Success`
- `FocusNotReturned`
- `InputBlocked`
- `UnknownError`

3) Use result-based UX
- Show concise, user-safe status when delivery fails.
- Preserve detailed diagnostics only in debug builds.

Decision record

- For Store submission readiness, keep the current runtime behavior unchanged.
- Defer end-to-end delivery acknowledgement to a later milestone.

Store Notes to Certification (direct launch clarification)

- This app is intended to be used from Xbox Game Bar (`Win+G`).
- The primary experience is the Game Bar widget UI.
- When launched directly from Start, the app may open Xbox Game Bar context as part of its shortcut handoff flow.
- All shortcut actions are user-initiated from visible buttons, and the helper runs on demand and exits immediately.

