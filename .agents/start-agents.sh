#!/bin/bash
ROLE=${1:-lead}
cd ~/Developer/volk

case $ROLE in
    lead)   echo "🎯 LEAD başlatılıyor..." ;;
    dev)    echo "💻 DEV başlatılıyor..." ;;
    review) echo "🧪 REVIEW başlatılıyor..." ;;
    *)      echo "❌ Geçersiz: $ROLE (lead|dev|review)"; exit 1 ;;
esac

claude --dangerously-skip-permissions --system-prompt "$(cat .agents/prompts/${ROLE}.md)"
