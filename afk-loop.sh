#!/bin/bash
# AFK loop: one Beads ticket per omp iteration. Streams live, saves everything.
# Usage: ./afk-loop.sh <iterations> [--model <model>]
#
# Per iteration:
#   - `bd ready` picks the highest-priority ticket; claims it
#   - streams assistant text to the terminal in real time (jq on --mode json)
#   - saves the full NDJSON event stream to .scratch/kwafee/convos/iter-N.jsonl
#   - prints the omp session id, resumable with: omp --resume <id>
#   - closes the ticket only when the agent reports <promise>DONE</promise>
#
# Watch live:      terminal stream, or tail -f .scratch/kwafee/convos/iter-N.jsonl
# Hop in:          Ctrl+C the loop, then: omp --resume <sid>
#                  Safe between iterations only — session files are single-writer.
set -euo pipefail

BOLD='\033[1m'
DIM='\033[2m'
RED='\033[31m'
GREEN='\033[32m'
YELLOW='\033[33m'
CYAN='\033[36m'
RESET='\033[0m'

iterations="${1:?Usage: $0 <iterations> [--model <model>]}"
model="${2:-}"

LOGDIR=".scratch/kwafee/convos"
mkdir -p "$LOGDIR"

STREAM_TEXT='select(.type == "message_update" and .assistantMessageEvent.type == "text_delta") | .assistantMessageEvent.delta // empty'
SESSION_ID='select(.type == "session") | .id // empty'

for ((i=1; i<=$iterations; i++)); do
  echo -e "${CYAN}${BOLD}━━━ Iteration ${i}/${iterations} ━━━ $(date +%H:%M:%S) ${RESET}"

  ticket=$(bd ready --claim --json 2>/dev/null)
  ticket_id=$(echo "$ticket" | jq -r '.[0].id // empty' 2>/dev/null)
  if [ -z "$ticket_id" ]; then
    echo -e "${GREEN}No ready ticket. All claimable work done.${RESET}"
    exit 0
  fi
  ticket_title=$(echo "$ticket" | jq -r '.[0].title // empty')
  echo -e "${YELLOW}${BOLD}Ticket:${RESET} ${BOLD}${ticket_id}${RESET} - ${ticket_title}"
  touch progress.txt

  tmp=$(mktemp)
  trap 'rm -f "$tmp"' EXIT

  omp -p --mode json ${model:+--model "$model"} @CONTEXT.md @.scratch/kwafee/DECISIONS.md @.scratch/kwafee/EVIDENCE.md @.scratch/kwafee/spec.md @.scratch/kwafee/architecture-spec.md \
    "Claimed ticket $ticket_id: $ticket_title.
     Start by running: bd show $ticket_id
     Implement the ticket's description and acceptance_criteria exactly. ONLY WORK ON THIS SINGLE TASK.
     1. Run your tests and type checks.
     2. Update the spec and progress.txt with what was done.
     3. Commit your changes.
     4. Output ONLY <promise>DONE</promise> when the ticket is complete and committed, or <promise>FAILED</promise> if you cannot finish it." \
  | grep --line-buffered '^{' \
  | tee "$tmp" "$LOGDIR/iter-$i.jsonl" \
  | jq --unbuffered -rj "$STREAM_TEXT"

  sid=$(jq -r "$SESSION_ID" "$tmp" | head -1)
  echo
  echo -e "${DIM}━━━ iter ${i} done ━━━${RESET} ${BOLD}session ${sid}${RESET} ${DIM}(saved ${LOGDIR}/iter-${i}.jsonl)${RESET}"

  if grep -q "<promise>DONE</promise>" "$tmp"; then
    bd close "$ticket_id"
    echo -e "${GREEN}${BOLD}Ticket ${ticket_id} closed. Loop complete after ${i} iterations.${RESET}"
    exit 0
  fi
  if grep -q "<promise>FAILED</promise>" "$tmp"; then
    echo -e "${RED}${BOLD}Ticket ${ticket_id} failed.${RESET} Left claimed for human triage (bd show ${ticket_id})."
    exit 1
  fi
  # No promise: ticket left open for the next iteration.
done