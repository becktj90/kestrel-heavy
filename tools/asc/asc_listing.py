#!/usr/bin/env python3
"""Read or apply the Kestrel Heavy App Store Connect listing (tools/asc/listing-1.0.json).

  python3 tools/asc/asc_listing.py read     # GET only: prints version state, listing fields, age rating, price, availability, screenshots
  python3 tools/asc/asc_listing.py apply    # PATCH/POST the listing, then reads everything back

Needs ASC_KEY_ID, ASC_ISSUER_ID and ASC_KEY_P8 (base64 of the .p8, as stored in the repo's Actions secrets) in the environment.
Never prints the key, the token or the issuer. Never submits for review, never creates a version, never attaches a build,
never touches signing, certificates, profiles, icons or in-app purchases. App Privacy, the Paid Apps agreement and the EU DSA
trader status are not in the public API and stay browser steps.
"""
import base64, hashlib, json, os, re, sys, time, urllib.error, urllib.parse, urllib.request

from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec, utils

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CFG = json.load(open(os.path.join(ROOT, "tools/asc/listing-1.0.json"), encoding="utf-8"))
API = "https://api.appstoreconnect.apple.com"
EDITABLE = {"PREPARE_FOR_SUBMISSION", "DEVELOPER_REJECTED", "REJECTED", "METADATA_REJECTED", "INVALID_BINARY", "DEVELOPER_REMOVED_FROM_SALE"}
FREQ = ["alcoholTobaccoOrDrugUseOrReferences", "contests", "gamblingSimulated", "gunsOrOtherWeapons", "horrorOrFearThemes",
        "matureOrSuggestiveThemes", "medicalOrTreatmentInformation", "profanityOrCrudeHumor", "sexualContentGraphicAndNudity",
        "sexualContentOrNudity", "violenceCartoonOrFantasy", "violenceRealistic", "violenceRealisticProlongedGraphicOrSadistic"]
BOOLS = ["advertising", "ageAssurance", "gambling", "healthOrWellnessTopics", "lootBox", "messagingAndChat", "parentalControls",
         "unrestrictedWebAccess", "userGeneratedContent"]
BANNED = [r"game\s*center", r"leaderboard", r"blue\s*origin", r"new\s*glenn", r"jacklyn", r"\bLC-36\b"]

_tok = [None, 0]


def token():
    if _tok[0] and time.time() < _tok[1] - 60:
        return _tok[0]
    enc = lambda b: base64.urlsafe_b64encode(b).rstrip(b"=").decode()
    now = int(time.time())
    head = {"alg": "ES256", "kid": os.environ["ASC_KEY_ID"], "typ": "JWT"}
    body = {"iss": os.environ["ASC_ISSUER_ID"], "iat": now, "exp": now + 1000, "aud": "appstoreconnect-v1"}
    msg = enc(json.dumps(head).encode()) + "." + enc(json.dumps(body).encode())
    key = serialization.load_pem_private_key(base64.b64decode(os.environ["ASC_KEY_P8"]), password=None)
    r, s = utils.decode_dss_signature(key.sign(msg.encode(), ec.ECDSA(hashes.SHA256())))
    _tok[0], _tok[1] = msg + "." + enc(r.to_bytes(32, "big") + s.to_bytes(32, "big")), now + 1000
    return _tok[0]


def req(method, path, body=None, ok_missing=False):
    url = path if path.startswith("http") else API + path
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method,
                               headers={"Authorization": "Bearer " + token(), "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(r, timeout=120) as resp:
            raw = resp.read()
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        txt = e.read().decode(errors="replace")
        if ok_missing and e.code == 404:
            return None
        raise RuntimeError(f"{method} {path.split('?')[0]} -> HTTP {e.code}: {txt[:1500]}")


def get(path, **q):
    return req("GET", path + ("?" + urllib.parse.urlencode(q) if q else ""))


def all_pages(path, **q):
    out, res = [], get(path, **q)
    while True:
        out += res.get("data", [])
        nxt = res.get("links", {}).get("next")
        if not nxt:
            return out, res.get("included", [])
        res = req("GET", nxt)


def show(title, obj):
    print(f"\n== {title}\n" + json.dumps(obj, indent=2, ensure_ascii=False), flush=True)


def lint():
    vl, ai = CFG["versionLocalization"], CFG["appInfo"]
    limits = [("name", ai["name"], 30), ("subtitle", ai["subtitle"], 30), ("promotionalText", vl["promotionalText"], 170),
              ("description", vl["description"], 4000), ("reviewNotes", CFG["reviewNotes"], 4000)]
    bad = [f"{k} {len(v)}>{n}" for k, v, n in limits if len(v) > n]
    if len(vl["keywords"].encode()) > 100:
        bad.append("keywords > 100 bytes")
    for k, v in [*[(k, v) for k, v, _ in limits], ("keywords", vl["keywords"])]:
        for pat in BANNED:
            if re.search(pat, v, re.I):
                bad.append(f"{k} mentions /{pat}/")
    if "AI-generated" not in vl["description"]:
        bad.append("description lost the AI-voice disclosure")
    if bad:
        raise SystemExit("listing lint failed: " + "; ".join(bad))
    print("listing lint OK (limits, no Game Center, no launch-provider names, AI-voice disclosure present)")


def find_app():
    apps = get("/v1/apps", **{"filter[bundleId]": CFG["bundleId"]})["data"]
    apps = [a for a in apps if a["attributes"]["bundleId"] == CFG["bundleId"]]
    if len(apps) != 1:
        raise SystemExit(f"expected one app for {CFG['bundleId']}, got {len(apps)}")
    return apps[0]


def pick_version(app_id):
    vers = get(f"/v1/apps/{app_id}/appStoreVersions", **{"filter[platform]": "IOS", "limit": 20})["data"]
    show("iOS versions", [{"id": v["id"], "version": v["attributes"]["versionString"], "state": v["attributes"].get("appStoreState"),
                           "versionState": v["attributes"].get("appVersionState"), "releaseType": v["attributes"].get("releaseType"),
                           "copyright": v["attributes"].get("copyright"), "created": v["attributes"].get("createdDate")} for v in vers])
    ed = [v for v in vers if v["attributes"].get("appStoreState") in EDITABLE]
    return ed[0] if ed else None


def pick_appinfo(app_id):
    infos = get(f"/v1/apps/{app_id}/appInfos")["data"]
    show("appInfos", [{"id": i["id"], "state": i["attributes"].get("state") or i["attributes"].get("appStoreState"),
                       "ageRating": i["attributes"].get("appStoreAgeRating"), "kidsAgeBand": i["attributes"].get("kidsAgeBand")} for i in infos])
    live = {"READY_FOR_DISTRIBUTION", "READY_FOR_SALE", "REPLACED_WITH_NEW_INFO"}
    ed = [i for i in infos if (i["attributes"].get("state") or i["attributes"].get("appStoreState")) not in live]
    return ed[0] if ed else infos[0]


def loc_for(items, locale):
    return next((x for x in items if x["attributes"]["locale"] == locale), None)


def read_all(app):
    app_id, loc = app["id"], CFG["locale"]
    a = app["attributes"]
    show("app", {k: a.get(k) for k in ["name", "bundleId", "sku", "primaryLocale", "contentRightsDeclaration", "isOrEverWasMadeForKids"]})
    info = pick_appinfo(app_id)
    il = loc_for(get(f"/v1/appInfos/{info['id']}/appInfoLocalizations")["data"], loc)
    show("appInfoLocalization " + loc, il and {k: il["attributes"].get(k) for k in ["name", "subtitle", "privacyPolicyUrl", "privacyChoicesUrl"]})
    cats = {}
    for rel in ["primaryCategory", "primarySubcategoryOne", "primarySubcategoryTwo", "secondaryCategory"]:
        d = req("GET", f"/v1/appInfos/{info['id']}/{rel}", ok_missing=True)
        cats[rel] = (d or {}).get("data", {}) and d["data"]["id"]
    show("categories", cats)
    ar = req("GET", f"/v1/appInfos/{info['id']}/ageRatingDeclaration", ok_missing=True)
    show("ageRatingDeclaration", ar and ar["data"]["attributes"])
    ver = pick_version(app_id)
    if ver:
        vid = ver["id"]
        vl = loc_for(get(f"/v1/appStoreVersions/{vid}/appStoreVersionLocalizations")["data"], loc)
        show("versionLocalization " + loc, vl and {k: vl["attributes"].get(k) for k in
             ["description", "keywords", "promotionalText", "marketingUrl", "supportUrl", "whatsNew"]})
        rd = req("GET", f"/v1/appStoreVersions/{vid}/appStoreReviewDetail", ok_missing=True)
        if rd and rd.get("data"):
            ra = rd["data"]["attributes"]
            show("appStoreReviewDetail", {"notes": ra.get("notes"), "demoAccountRequired": ra.get("demoAccountRequired"),
                                          "contactSet": all(ra.get(k) for k in ["contactFirstName", "contactLastName", "contactEmail", "contactPhone"])})
        else:
            show("appStoreReviewDetail", None)
        b = req("GET", f"/v1/appStoreVersions/{vid}/build", ok_missing=True)
        show("attached build", b and b.get("data") and b["data"]["attributes"].get("version"))
        if vl:
            sets = get(f"/v1/appStoreVersionLocalizations/{vl['id']}/appScreenshotSets")["data"]
            summary = []
            for st in sets:
                shots = get(f"/v1/appScreenshotSets/{st['id']}/appScreenshots")["data"]
                summary.append({"type": st["attributes"]["screenshotDisplayType"], "count": len(shots),
                                "files": [(s["attributes"].get("fileName"), (s["attributes"].get("assetDeliveryState") or {}).get("state"),
                                           s["attributes"].get("imageAsset") and f"{s['attributes']['imageAsset'].get('width')}x{s['attributes']['imageAsset'].get('height')}")
                                          for s in shots]})
            show("screenshot sets " + loc, summary)
    builds = get("/v1/builds", **{"filter[app]": app_id, "sort": "-uploadedDate", "limit": 5, "include": "preReleaseVersion"})
    pre = {i["id"]: i["attributes"].get("version") for i in builds.get("included", []) if i["type"] == "preReleaseVersions"}
    show("recent builds", [{"build": x["attributes"]["version"], "marketing": pre.get(((x.get("relationships") or {}).get("preReleaseVersion") or {}).get("data", {}).get("id")),
                            "processing": x["attributes"]["processingState"], "expired": x["attributes"].get("expired"),
                            "uploaded": x["attributes"]["uploadedDate"]} for x in builds["data"]])
    ps = req("GET", f"/v1/apps/{app_id}/appPriceSchedule?include=baseTerritory", ok_missing=True)
    if ps and ps.get("data"):
        base = (ps["data"].get("relationships", {}).get("baseTerritory", {}).get("data") or {}).get("id")
        prices, inc = all_pages(f"/v1/appPriceSchedules/{ps['data']['id']}/manualPrices", include="appPricePoint,territory", limit=50)
        pts = {i["id"]: i["attributes"].get("customerPrice") for i in inc if i["type"] == "appPricePoints"}
        show("price schedule", {"baseTerritory": base, "manualPrices": [
            {"territory": (p["relationships"].get("territory", {}).get("data") or {}).get("id"), "start": p["attributes"].get("startDate"),
             "end": p["attributes"].get("endDate"), "customerPrice": pts.get((p["relationships"].get("appPricePoint", {}).get("data") or {}).get("id"))}
            for p in prices]})
    else:
        show("price schedule", None)
    av = req("GET", f"/v1/apps/{app_id}/appAvailabilityV2", ok_missing=True)
    if av and av.get("data"):
        terr, _ = all_pages(f"/v2/appAvailabilities/{av['data']['id']}/territoryAvailabilities", limit=200, include="territory")
        on = sorted(t["relationships"]["territory"]["data"]["id"] for t in terr if t["attributes"].get("available"))
        show("availability", {"availableInNewTerritories": av["data"]["attributes"].get("availableInNewTerritories"),
                              "availableCount": len(on), "CHN": "CHN" in on, "VNM": "VNM" in on, "USA": "USA" in on})
    else:
        show("availability", None)
    return info, il, ver


def upload_set(vl_id, display_type, folder):
    files = sorted(f for f in os.listdir(os.path.join(ROOT, folder)) if f.endswith(".png"))
    want = []
    for f in files:
        data = open(os.path.join(ROOT, folder, f), "rb").read()
        want.append((f, data, hashlib.md5(data).hexdigest()))
    sets = get(f"/v1/appStoreVersionLocalizations/{vl_id}/appScreenshotSets")["data"]
    st = next((s for s in sets if s["attributes"]["screenshotDisplayType"] == display_type), None)
    if st is None:
        st = req("POST", "/v1/appScreenshotSets", {"data": {"type": "appScreenshotSets", "attributes": {"screenshotDisplayType": display_type},
                 "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": vl_id}}}}})["data"]
    have = get(f"/v1/appScreenshotSets/{st['id']}/appScreenshots")["data"]
    if [(h["attributes"].get("fileName"), h["attributes"].get("sourceFileChecksum")) for h in have] == [(f, m) for f, _, m in want] and \
            all((h["attributes"].get("assetDeliveryState") or {}).get("state") == "COMPLETE" for h in have):
        print(f"{display_type}: already has the {len(want)} expected screenshots, skipping")
        return
    for h in have:
        req("DELETE", f"/v1/appScreenshots/{h['id']}")
    print(f"{display_type}: removed {len(have)} old screenshot(s)")
    ids = []
    for f, data, md5 in want:
        s = req("POST", "/v1/appScreenshots", {"data": {"type": "appScreenshots", "attributes": {"fileName": f, "fileSize": len(data)},
                "relationships": {"appScreenshotSet": {"data": {"type": "appScreenshotSets", "id": st["id"]}}}}})["data"]
        for op in s["attributes"]["uploadOperations"]:
            chunk = data[op["offset"]:op["offset"] + op["length"]]
            r = urllib.request.Request(op["url"], data=chunk, method=op["method"],
                                       headers={h["name"]: h["value"] for h in op.get("requestHeaders", [])})
            urllib.request.urlopen(r, timeout=300).read()
        req("PATCH", f"/v1/appScreenshots/{s['id']}", {"data": {"type": "appScreenshots", "id": s["id"],
            "attributes": {"uploaded": True, "sourceFileChecksum": md5}}})
        ids.append(s["id"])
    for _ in range(60):
        states = [(get(f"/v1/appScreenshots/{i}")["data"]["attributes"].get("assetDeliveryState") or {}) for i in ids]
        if all(x.get("state") in ("COMPLETE", "FAILED") for x in states):
            break
        time.sleep(5)
    failed = [(want[n][0], x.get("errors")) for n, x in enumerate(states) if x.get("state") != "COMPLETE"]
    req("PATCH", f"/v1/appScreenshotSets/{st['id']}/relationships/appScreenshots",
        {"data": [{"type": "appScreenshots", "id": i} for i in ids]})
    print(f"{display_type}: uploaded {len(ids)}, not complete: {failed or 'none'}")
    if failed:
        raise RuntimeError(f"{display_type} screenshot processing failed: {failed}")


def apply(app):
    errors = []

    def step(name, fn):
        try:
            fn()
            print(f"OK   {name}", flush=True)
        except Exception as e:  # keep going; report all failures at the end
            errors.append(f"{name}: {e}")
            print(f"FAIL {name}: {e}", flush=True)

    app_id, loc = app["id"], CFG["locale"]
    info, il, ver = read_all(app)
    if app["attributes"].get("contentRightsDeclaration") is None:
        step("content rights", lambda: req("PATCH", f"/v1/apps/{app_id}", {"data": {"type": "apps", "id": app_id,
             "attributes": {"contentRightsDeclaration": CFG["contentRightsDeclaration"]}}}))
    ai = CFG["appInfo"]
    if il:
        step("app info localization (name, subtitle, privacy URL)", lambda: req("PATCH", f"/v1/appInfoLocalizations/{il['id']}",
             {"data": {"type": "appInfoLocalizations", "id": il["id"], "attributes": ai}}))
    else:
        errors.append(f"no {loc} appInfoLocalization")
    rel = {k: {"data": {"type": "appCategories", "id": v}} for k, v in CFG["categories"].items()}
    step("categories", lambda: req("PATCH", f"/v1/appInfos/{info['id']}", {"data": {"type": "appInfos", "id": info["id"], "relationships": rel}}))

    def age():
        cur = get(f"/v1/appInfos/{info['id']}/ageRatingDeclaration")["data"]
        attrs = {k: "NONE" for k in FREQ if k in cur["attributes"]}
        attrs.update({k: False for k in BOOLS if k in cur["attributes"]})
        if "kidsAgeBand" in cur["attributes"]:
            attrs["kidsAgeBand"] = None
        unknown = sorted(set(cur["attributes"]) - set(attrs) - {"ageRatingOverride", "ageRatingOverrideV2", "koreaAgeRatingOverride",
                                                                "developerAgeRatingInfoUrl", "seventeenPlus", "unrestrictedWebAccess"})
        if unknown:
            print("age rating: left unchanged (unknown fields):", unknown)
        req("PATCH", f"/v1/ageRatingDeclarations/{cur['id']}", {"data": {"type": "ageRatingDeclarations", "id": cur["id"], "attributes": attrs}})
    step("age rating declaration", age)

    if ver is None:
        errors.append("no editable iOS version (not creating one)")
    else:
        vid = ver["id"]
        step("version copyright + manual release", lambda: req("PATCH", f"/v1/appStoreVersions/{vid}", {"data": {"type": "appStoreVersions",
             "id": vid, "attributes": CFG["version"]}}))
        vls = get(f"/v1/appStoreVersions/{vid}/appStoreVersionLocalizations")["data"]
        vl = loc_for(vls, loc)
        if vl is None:
            errors.append(f"no {loc} version localization")
        else:
            step("version localization (description, keywords, promo, URLs)", lambda: req("PATCH", f"/v1/appStoreVersionLocalizations/{vl['id']}",
                 {"data": {"type": "appStoreVersionLocalizations", "id": vl["id"], "attributes": CFG["versionLocalization"]}}))
            for dt, folder in CFG["screenshots"].items():
                step(f"screenshots {dt}", lambda dt=dt, folder=folder: upload_set(vl["id"], dt, folder))
        rd = req("GET", f"/v1/appStoreVersions/{vid}/appStoreReviewDetail", ok_missing=True)
        if rd and rd.get("data"):
            step("review notes", lambda: req("PATCH", f"/v1/appStoreReviewDetails/{rd['data']['id']}", {"data": {"type": "appStoreReviewDetails",
                 "id": rd["data"]["id"], "attributes": {"notes": CFG["reviewNotes"], "demoAccountRequired": False}}}))
        else:
            errors.append("no App Review detail record (contact info must be entered in the browser first; notes not set)")

    def price():
        want = CFG["price"]
        pts, _ = all_pages(f"/v1/apps/{app_id}/appPricePoints", **{"filter[territory]": want["baseTerritory"], "limit": 200})
        pp = next((p for p in pts if p["attributes"].get("customerPrice") == want["customerPrice"]), None)
        if pp is None:
            raise RuntimeError("no USD 0.99 price point")
        req("POST", "/v1/appPriceSchedules", {"data": {"type": "appPriceSchedules", "relationships": {
            "app": {"data": {"type": "apps", "id": app_id}},
            "baseTerritory": {"data": {"type": "territories", "id": want["baseTerritory"]}},
            "manualPrices": {"data": [{"type": "appPrices", "id": "${p1}"}]}}},
            "included": [{"type": "appPrices", "id": "${p1}", "attributes": {"startDate": None},
                          "relationships": {"appPricePoint": {"data": {"type": "appPricePoints", "id": pp["id"]}}}}]})
    ps = req("GET", f"/v1/apps/{app_id}/appPriceSchedule?include=baseTerritory", ok_missing=True)
    current = None
    if ps and ps.get("data"):
        prices, inc = all_pages(f"/v1/appPriceSchedules/{ps['data']['id']}/manualPrices", include="appPricePoint,territory", limit=50)
        cur = [p for p in prices if p["attributes"].get("startDate") is None and p["attributes"].get("endDate") is None]
        pts = {i["id"]: i["attributes"].get("customerPrice") for i in inc if i["type"] == "appPricePoints"}
        current = [(p["relationships"]["territory"]["data"]["id"], pts.get(p["relationships"]["appPricePoint"]["data"]["id"])) for p in cur]
    if current == [(CFG["price"]["baseTerritory"], CFG["price"]["customerPrice"])]:
        print("price already USD 0.99, skipping")
    else:
        step("price USD 0.99", price)

    def territories():
        av = get(f"/v1/apps/{app_id}/appAvailabilityV2")["data"]
        terr, _ = all_pages(f"/v2/appAvailabilities/{av['id']}/territoryAvailabilities", limit=200, include="territory")
        for t in terr:
            code = t["relationships"]["territory"]["data"]["id"]
            if code in CFG["unavailableTerritories"] and t["attributes"].get("available"):
                req("PATCH", f"/v1/territoryAvailabilities/{t['id']}", {"data": {"type": "territoryAvailabilities", "id": t["id"],
                    "attributes": {"available": False}}})
                print("made unavailable:", code)
    step("remove China mainland + Vietnam", territories)

    print("\n######## READ-BACK AFTER APPLY ########")
    read_all(get(f"/v1/apps/{app_id}")["data"])
    if errors:
        print("\nERRORS:\n- " + "\n- ".join(errors))
        sys.exit(1)


def main():
    mode = (sys.argv[1:] or ["read"])[0]
    lint()
    if mode == "lint":
        return
    app = find_app()
    if mode == "read":
        read_all(app)
    elif mode == "apply":
        apply(app)
    else:
        raise SystemExit("mode must be lint, read or apply")


if __name__ == "__main__":
    main()
