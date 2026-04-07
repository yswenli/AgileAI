# Script Demo Checklist

- Confirm whether the user wants explanation only or an actual demo.
- State that this is a prompt-based skill, not a native JS/Python skill runtime.
- Show the exact command before any execution request.
- Keep commands harmless and quick.
- Mention environment assumptions like `node` or `python` availability.
- If `run_local_command` is available, note that execution is approval-gated.
- If tools are unavailable, fall back to manual commands or inline script text.
- Never claim execution succeeded without a tool result.
