"""Expose bounded, test-only cache metadata in public CI annotations."""

import json
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
trace = root / "TestResults/cache-diagnostics.log"
report = root / "TestResults/backend-tests.trx"


def public_metadata(row):
    """Never publish request URLs, keys, values, or exception messages."""
    result = {key: row[key] for key in (
        "phase", "generation", "utcTicks", "cacheTicks", "cacheId",
        "resetCacheId", "localCount", "name",
    ) if key in row and isinstance(row[key], (int, type(None)))}
    for key in ("phase", "name"):
        if key in row and isinstance(row[key], str) and row[key].replace("-", "").isalnum():
            result[key] = row[key]
    result["tagTimes"] = {
        tag: int(value) for tag, value in (row.get("tags") or {}).items()
        if tag in {"departments", "locations"} and value.isdigit()
    }
    result["entries"] = [
        {
            "treeRoot": entry.get("key") == "department_tree:roots",
            "created": entry.get("created"),
            "tagCount": entry.get("tagCount"),
        }
        for entry in row.get("entries") or []
    ]
    return result


if trace.exists() and report.exists():
    records = [json.loads(line) for line in trace.read_text().splitlines()]
    namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    failed_classes = sorted({
        result.attrib["testName"].split(".")[-2]
        for result in ET.parse(report).findall("t:Results/t:UnitTestResult", namespace)
        if result.attrib.get("outcome") == "Failed"
    })
    known_classes = {
        "DepartmentCachingTests", "DepartmentTreeTests", "GetDepartmentsTests",
        "GetLocationsTests", "GetTopDepartmentsByPositionsTests", "UpdateDepartmentActivityTests",
    }
    for test in sorted(set(failed_classes) & known_classes):
        relevant = [public_metadata(row) for row in records if row.get("test") == test]
        text = json.dumps(relevant, separators=(",", ":"))[:60000]
        escaped = text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
        print(f"::notice title=Cache diagnostics {test}::{escaped}")
    print(f"Cache diagnostics: {len(records)} events, {len(failed_classes)} failed classes.")
