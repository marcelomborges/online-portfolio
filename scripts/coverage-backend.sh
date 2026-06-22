#!/usr/bin/env bash
# Runs backend tests with Coverlet and generates an HTML coverage report.
# Output: backend/TestResults/ (gitignored)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND="$ROOT/backend"
TEST_RESULTS="$BACKEND/TestResults"
REPORT_DIR="$TEST_RESULTS/CoverageReport"

cd "$BACKEND"

echo "Restoring dotnet tools (ReportGenerator)..."
dotnet tool restore

if [[ -d "$TEST_RESULTS" ]]; then
  echo "Cleaning previous TestResults..."
  rm -rf "$TEST_RESULTS"
fi

echo "Running tests with coverage..."
dotnet test OnlinePortfolio.Api.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory "$TEST_RESULTS"

mapfile -t COVERAGE_FILES < <(find "$TEST_RESULTS" -name 'coverage.cobertura.xml')
if [[ ${#COVERAGE_FILES[@]} -eq 0 ]]; then
  echo "No coverage.cobertura.xml found under $TEST_RESULTS" >&2
  exit 1
fi

REPORTS=$(IFS=';'; echo "${COVERAGE_FILES[*]}")

echo "Generating HTML report..."
dotnet reportgenerator \
  "-reports:$REPORTS" \
  "-targetdir:$REPORT_DIR" \
  "-reporttypes:Html"

echo ""
echo "Coverage report: $REPORT_DIR/index.html"
echo "(TestResults/ is gitignored — nothing is committed to GitHub)"

if command -v xdg-open >/dev/null 2>&1; then
  xdg-open "$REPORT_DIR/index.html"
elif command -v open >/dev/null 2>&1; then
  open "$REPORT_DIR/index.html"
fi
