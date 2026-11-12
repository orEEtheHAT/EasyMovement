# Changelog

## 5.1.0 — 2026-11-12

- Added independent, idempotent `AcquireInputBlock(owner)` handles, `InputBlocked` and `InputBlockedChanged`. Disabled/destroyed owners and controller pooling release their own blocks while scripted motor calls remain available.
- The standalone FPS reader respects blocks in Update and every callback, releases only its held intents once, and requires held buttons to be released before resuming. It does not overwrite another movement provider with zero input; sprint, crouch and NoClip behavior are preserved.
- Added validated `FirstPersonCharacterState` capture/restore for world pose, view pitch and supported gait requests, without automatic storage or input-settings persistence.
- Added optional `CharacterStepController.ContextProvider` with inspector diagnostics and legacy fallback when no active valid provider is assigned. The existing EasyFeedbacks dependency now requires 1.8.0 for `IFeedbackContextProvider`.
- Added overlapping-owner, callback, external-motion, disable/destroy and footstep-routing regressions. Allocation checks now use a calibrated main-thread GC.Alloc recorder with a positive control; the earlier per-thread .NET counter result is not allocation evidence.
