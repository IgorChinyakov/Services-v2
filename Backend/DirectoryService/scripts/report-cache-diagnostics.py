"""Expose bounded, test-only cache metadata in public CI annotations."""

import json
from datetime import datetime, timezone
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
        for entry in row.get("entries") or [] if entry.get("key") == "department_tree:roots"
    ]
    return result


if trace.exists() and report.exists():
    records = [json.loads(line) for line in trace.read_text().splitlines()]
    namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    known_classes = {
        "DepartmentCachingTests", "DepartmentTreeTests", "GetDepartmentsTests",
        "GetLocationsTests", "GetTopDepartmentsByPositionsTests", "UpdateDepartmentActivityTests",
    }
    failures = [result for result in ET.parse(report).findall("t:Results/t:UnitTestResult", namespace)
                if result.attrib.get("outcome") == "Failed"]
    for result in failures[:9]:
        test = result.attrib["testName"].split(".")[-2]
        if test not in known_classes:
            continue
        end = datetime.fromisoformat(result.attrib["endTime"])
        ticks = int((end - datetime(1, 1, 1, tzinfo=timezone.utc)).total_seconds() * 10000000)
        candidates = [row for row in records if row.get("test") == test
                      and row.get("utcTicks", 0) <= ticks + 10000]
        generation = max((row.get("generation", 0) for row in candidates), default=0)
        relevant = [row for row in candidates if row.get("generation", 0) >= generation - 1]
        metadata = [public_metadata(row) for row in relevant if row.get("phase") != "cache-event"]
        events = [row.get("name") for row in relevant if row.get("phase") == "cache-event"]
        # GitHub truncates each annotation at 4096 characters.
        summaries = {"snapshots": metadata, "events": events}
        text = json.dumps(summaries, separators=(",", ":"))
        if len(text) > 3900:
            metadata = [{k: v for k, v in row.items() if k not in {"utcTicks", "cacheTicks", "localCount"}}
                        for row in metadata]
            text = json.dumps({"snapshots": metadata, "events": events}, separators=(",", ":"))
        text = text[:3900]
        escaped = text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
        print(f"::notice title=Cache diagnostics {test} generation {generation}::{escaped}")
    print(f"Cache diagnostics: {len(records)} events, {len(failures)} failures.")
