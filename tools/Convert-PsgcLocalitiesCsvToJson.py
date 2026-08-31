#!/usr/bin/env python3
"""Convert generated PSGC City/Municipality CSV into the committed ExItS JSON snapshot."""

from __future__ import annotations

import csv
import json
import os
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CSV_PATH = ROOT / "tools" / ".generated" / "psgc-localities-2026-06-30.csv"
OUT_PATH = (
    ROOT
    / "src"
    / "Platform"
    / "ExItS.Platform.Infrastructure"
    / "ReferenceData"
    / "Philippines"
    / "psgc-localities-2026-06-30.json"
)


def main() -> None:
    if not CSV_PATH.is_file():
        raise SystemExit(f"Missing {CSV_PATH}. Run tools/Generate-PsgcLocalitiesSnapshot.R first.")

    localities = []
    with CSV_PATH.open(newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            localities.append(
                {
                    "psgcCode": row["psgcCode"].strip(),
                    "name": row["name"].strip(),
                    "localityType": row["localityType"].strip(),
                    "regionCode": row["regionCode"].strip(),
                    "regionName": row["regionName"].strip(),
                    "provinceCode": row["provinceCode"].strip() or None,
                    "provinceName": row["provinceName"].strip() or None,
                }
            )

    codes = [x["psgcCode"] for x in localities]
    if len(codes) != len(set(codes)):
        raise SystemExit("Duplicate PSGC codes in CSV.")
    for row in localities:
        if row["localityType"] not in ("City", "Municipality"):
            raise SystemExit(f"Unsupported type: {row['localityType']}")
        if not (row["psgcCode"] and row["name"] and row["regionCode"] and row["regionName"]):
            raise SystemExit(f"Incomplete row: {row}")

    payload = {
        "metadata": {
            "source": "Philippine Statistics Authority",
            "dataset": "Philippine Standard Geographic Code",
            "asOf": "2026-06-30",
            "release": "2Q 2026",
            "country": "PH",
            "datasetVersion": "PSGC-2026-06-30",
            "geographicLevels": ["City", "Municipality"],
            "recordCount": len(localities),
            "generatedAtUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
            "notes": (
                "City and Municipality only. Province null when not applicable. "
                "Runtime does not call psa.gov.ph."
            ),
        },
        "localities": localities,
    }

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with OUT_PATH.open("w", encoding="utf-8", newline="\n") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print(f"Wrote {OUT_PATH} count={len(localities)} bytes={OUT_PATH.stat().st_size}")


if __name__ == "__main__":
    main()
