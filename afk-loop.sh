#!/bin/bash
# AFK loop: one Beads ticket per omp iteration. Streams live, saves everything.
# Usage: ./afk-loop.sh <iterations> [--model <model>]
#
# Ticket picking (see docs/adr/0008-ticket-taxonomy-afk-loop.md):
#   `bd ready --exclude-type=epic` surfaces exactly the implementation
#   frontier: open + unclaimed + undeferred tasks/features with no active
#   blockers, priority-ordered. Artifact umbrellas are `epic` type, so they
#   never surface here — the loop claims only vertical-slice work tickets
#   (children) + the shift-seam feature ticket (kwaffee-6id), never specs.
#   Iterations stop at the first ticket that reports <promise>DONE</promise>.
#
# Per iteration:
#   - `bd ready` picks the highest-priority ticket; claims it
#   - streams the raw omp NDJSON to the terminal in real time (omp --mode json emits pure NDJSON; tee displays + saves)
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

SESSION_ID='select(.type == "session") | .id // empty'
TMP_FILES=()

for ((i=1; i<=$iterations; i++)); do
  echo -e "${CYAN}${BOLD}━━━ Iteration ${i}/${iterations} ━━━ $(date +%H:%M:%S) ${RESET}"

  # On a retry, reuse the same ticket (e.g. omp network failure aborted the
  # last run — it stays claimed, and `bd ready` would skip it forever).
  if [ -z "${retry_ticket_id:-}" ]; then
    ticket=$(bd ready --exclude-type=epic --json 2>/dev/null) || true
    ticket_id=$(echo "$ticket" | jq -r '.[0].id // empty' 2>/dev/null) || true
    if [ -z "$ticket_id" ]; then
      echo -e "${GREEN}No ready ticket. All claimable work done.${RESET}"
      exit 0
    fi
    ticket_title=$(echo "$ticket" | jq -r '.[0].title // empty') || true
    bd update "$ticket_id" --claim >/dev/null 2>&1 || true
  else
    ticket_id="$retry_ticket_id"
    ticket_title="$retry_ticket_title"
  fi
  echo -e "${YELLOW}${BOLD}Ticket:${RESET} ${BOLD}${ticket_id}${RESET} - ${ticket_title}"
  PROGDIR=".scratch/kwafee/progress"
  mkdir -p "$PROGDIR"
  progress_file="$PROGDIR/${ticket_id}.txt"
  touch "$progress_file"

  # Resolve the ticket's own spec from its body's "Spec:" pointer. The shared
  # spec files are already passed below; only add the per-ticket spec if it's
  # a different file.
  spec_line=$(bd show "$ticket_id" 2>/dev/null | grep -oE 'Spec: [^ )]+' | head -1 | cut -d' ' -f2) || true
  spec_arg=""
  spec_label="the shared spec"
  case "$spec_line" in
    ""|CONTEXT.md|.scratch/kwafee/DECISIONS.md|.scratch/kwafee/EVIDENCE.md|.scratch/kwafee/spec.md|.scratch/kwafee/architecture-spec.md)
      ;;
    *)
      if [ -f "$spec_line" ]; then
        spec_arg="@$spec_line"
        spec_label="$spec_line"
        echo -e "${DIM}Spec: ${spec_line}${RESET}"
      fi
      ;;
  esac

  tmp=$(mktemp)
  TMP_FILES+=("$tmp")
  trap 'rm -f "${TMP_FILES[@]}"' EXIT

  omp -p --mode json ${model:+--model "$model"} @CONTEXT.md @.scratch/kwafee/DECISIONS.md @.scratch/kwafee/EVIDENCE.md @.scratch/kwafee/spec.md @.scratch/kwafee/architecture-spec.md ${spec_arg:+$spec_arg} \
    "Claimed ticket $ticket_id: $ticket_title.
     Start by running: bd show $ticket_id
     Implement the ticket's description and acceptance_criteria exactly. ONLY WORK ON THIS SINGLE TASK.
     The ticket's own spec ($spec_label) is attached as a reference: follow its Implementation Decisions and Refined Gate. Do not take on other tickets' or artifacts' work.
     1. Run your tests and type checks.
     2. Update the spec and $progress_file with what was done.
     3. Commit your changes.
     4. Output ONLY <promise>DONE</promise> when the ticket is complete and committed, or <promise>FAILED</promise> if you cannot finish it." \
  | tee "$tmp" "$LOGDIR/iter-$i.jsonl" | jq -r
  omp_rc=${PIPESTATUS[0]}

  sid=$(jq -r "$SESSION_ID" "$tmp" | head -1) || true
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
  if [ "$omp_rc" -ne 0 ]; then
    echo -e "${YELLOW}omp exited non-zero ($omp_rc) on ${ticket_id} — retrying same ticket next iteration.${RESET}"
    retry_ticket_id="$ticket_id"
    retry_ticket_title="$ticket_title"
    continue
  fi
  unset retry_ticket_id retry_ticket_title
  rm -f "$tmp"
  # No promise: ticket left open for the next iteration.
done
