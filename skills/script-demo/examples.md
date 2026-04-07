# Script Demo Examples

## Example prompts

- "Show me how this project can demonstrate a Python-backed skill."
- "Use the script demo skill to explain the current limitation, then prepare a safe Node command."
- "Demonstrate a tiny Python calculation and tell me the exact command before execution."
- "If command execution is unavailable, just give me the JS snippet and shell command."

## Example response shape

1. State the limitation clearly: local skills are prompt-based in this repository.
2. Explain the indirect path: script behavior can be demonstrated through command-execution tools.
3. Present a harmless exact command.
4. If a tool such as `run_local_command` is available, ask to execute it through the tool path.
5. If not, provide the manual command and expected output.
