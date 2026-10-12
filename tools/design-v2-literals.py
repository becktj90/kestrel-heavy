#!/usr/bin/env python3
"""External literals for designer v2 interstage mass and Barrowman CP.

These formulas are the contract in docs/PHYSICS.md. They do not call Kestrel.Core.
`unity/Assets/Kestrel.Core.Tests/DesignV2Tests.cs` compares the C# results to this file.
"""
import json
import math
from pathlib import Path

PI = math.pi
G0 = 9.80665
AREAL = {"composite": 14.0, "al-li": 20.0, "steel": 45.0}
AREAL_DEFAULT = 20.0
STRUCTURAL = 0.045  # al-li
REF_SAFETY = 1.4
BULK = 1030.0  # kerolox
ULLAGE = 0.03
DOME = 0.33
ENGINE_MASS = 470.0
ENGINE_DIAMETER = 0.92
AVIONICS = 150.0  # standard
FAIRING_AREAL = 14.0  # composite
SEPARATION = 50.0
PAYLOAD = 500.0
FAIRING_LENGTH = 4.0
PROP = (20000.0, 5000.0)
FIN_COUNT = 4
FIN_SPAN = 1.2


def length_m(d_lower, d_upper):
    return 0.6 * max(d_lower, d_upper)


def lateral_area(d_lower, d_upper):
    length = length_m(d_lower, d_upper)
    r1, r2 = d_lower / 2.0, d_upper / 2.0
    slant = math.sqrt(length * length + (r1 - r2) * (r1 - r2))
    return PI * (r1 + r2) * slant


def derived_mass(material, d_lower, d_upper):
    return AREAL.get(material, AREAL_DEFAULT) * lateral_area(d_lower, d_upper)


def conical(d_aft, d_forward, length, d_ref):
    """Barrowman conical transition. x is metres aft of the forward end."""
    cna = 0.0
    x_from_forward = 0.0
    if d_ref <= 0 or d_aft <= 0 or d_forward <= 0:
        return cna, x_from_forward
    cna = 2.0 * ((d_aft / d_ref) ** 2 - (d_forward / d_ref) ** 2)
    if cna == 0.0 or length <= 0:
        return cna, x_from_forward
    ratio = d_forward / d_aft
    denom = 1.0 - ratio * ratio
    if denom != 0.0:
        x_from_forward = (length / 3.0) * (1.0 + (1.0 - ratio) / denom)
    return cna, x_from_forward


def bell(engine_diameter):
    b = engine_diameter * 1.6
    if b < 0.4:
        return 0.4
    if b > 4.0:
        return 4.0
    return b


def tank_length(prop_mass, diameter):
    volume = (prop_mass / BULK) * (1.0 + ULLAGE)
    area = PI * (diameter / 2.0) ** 2
    return volume / area + DOME * diameter


def fairing_mass(diameter):
    r = diameter / 2.0
    return 2.0 * PI * r * FAIRING_LENGTH * 0.85 * FAIRING_AREAL


def vehicle(d_lower, d_upper, fins, include_interstage_cone, prop=None, payload=None, fairing_length=None, fin_span=None):
    """Two-stage stations. +Y up, base of stage 1 at 0. cp_m is metres from the base."""
    prop = PROP if prop is None else prop
    payload = PAYLOAD if payload is None else payload
    fairing_length = FAIRING_LENGTH if fairing_length is None else fairing_length
    fin_span = FIN_SPAN if fin_span is None else fin_span
    diameters = (d_lower, d_upper)
    sf_scale = 1.0  # safety 1.4
    b = bell(ENGINE_DIAMETER)
    masses = []  # (mass, y)

    def add(mass, y):
        masses.append((mass, y))

    y = 0.0
    transitions = []
    for i, diameter in enumerate(diameters):
        r = diameter / 2.0
        bottom = i == 0
        top = i == len(diameters) - 1
        thrust = 0.6 * r + (0.0 if bottom else b * 0.7)
        tank = tank_length(prop[i], diameter)
        y_base = y + (b if bottom else 0.0)
        y_prop0 = y_base + thrust
        y_tank1 = y_prop0 + tank
        structure = prop[i] * STRUCTURAL * sf_scale
        y_engine = b / 2.0 if bottom else y_base - length_m(diameters[i - 1], diameter) / 2.0
        add(ENGINE_MASS, y_engine)
        add(structure, (y_base + y_tank1) / 2.0)
        if not top:
            inter = length_m(diameter, diameters[i + 1])
            add(SEPARATION, y_tank1)
            transitions.append({
                "y_forward": y_tank1 + inter,
                "length": inter,
                "d_forward": diameters[i + 1],
                "d_aft": diameter,
                "interstage": True,
            })
            y = y_tank1 + inter
        else:
            inter_mass = derived_mass("al-li", diameters[i - 1], diameter)
            add(inter_mass, y_base - length_m(diameters[i - 1], diameter) / 2.0)
            add(SEPARATION, y_tank1)
            add(AVIONICS, y_tank1)
            y = y_tank1
        add(prop[i], (y_prop0 + y_tank1) / 2.0)

    y_joint = y
    fair_d = d_upper
    transitions.append({
        "y_forward": y_joint,
        "length": 0.0,
        "d_forward": fair_d,
        "d_aft": d_upper,
        "interstage": False,
    })
    nose_frac = 0.45  # ogive
    add(2.0 * PI * (fair_d / 2.0) * fairing_length * 0.85 * FAIRING_AREAL, y_joint + fairing_length / 2.0)
    add(payload, y_joint + fairing_length * (1.0 - nose_frac) / 2.0)

    length = y_joint + fairing_length
    d_ref = max(d_lower, d_upper, fair_d)
    m_sum = sum(m for m, _ in masses)
    my = sum(m * yy for m, yy in masses)
    cg = 0.0 if m_sum == 0.0 else my / m_sum

    cna = 0.0
    cnx = 0.0

    def add_cn(cn, x_from_nose):
        nonlocal cna, cnx
        cna += cn
        cnx += cn * x_from_nose

    ln = fairing_length * nose_frac
    add_cn(2.0 * (fair_d / d_ref) ** 2, 0.466 * ln)
    for t in transitions:
        if t["interstage"] and not include_interstage_cone:
            continue
        cn, x_fwd = conical(t["d_aft"], t["d_forward"], t["length"], d_ref)
        if cn == 0.0:
            continue
        add_cn(cn, (length - t["y_forward"]) + x_fwd)
    if fins:
        span = fin_span
        cr = ct = span
        # Unswept square, trailing edge on the base. l_F is the span. x_t is 0.
        lf = span
        cna1 = (4.0 * FIN_COUNT * (span / d_ref) ** 2) / (1.0 + math.sqrt(1.0 + (2.0 * lf / (cr + ct)) ** 2))
        radius = d_lower / 2.0
        kfb = 1.0 + radius / (span + radius)
        xf = fin_cp_aft_of_root_le(cr, ct, 0.0)
        add_cn(kfb * cna1, length - (cr - xf))
    cp = length / 2.0 if cna == 0.0 else length - cnx / cna
    stab = (cg - cp) / d_ref if d_ref > 0 else 0.0
    ahead = cp > length
    return {
        "cg_m": cg,
        "cpRaw_m": cp,
        "cp_m": length if ahead else cp,
        "cpAheadOfNose": ahead,
        "stabilityCal": stab,
        "mass_kg": m_sum,
        "length_m": length,
        "refDiameter_m": d_ref,
    }


def fin_cp_aft_of_root_le(cr, ct, xt):
    """Barrowman quarter-chord of the MAC, metres aft of the root leading edge.

    A square fin with x_t = 0 is 0.25c. The trailing edge sits on the base, so the
    hangar station from the nose is length - (c_r - x_f), which is length - 0.75c.
    Measuring x_f forward from the trailing edge would land on the three-quarter chord.
    """
    total = cr + ct
    if total <= 0:
        return 0.0
    return (xt / 3.0) * (cr + 2.0 * ct) / total + (1.0 / 6.0) * (total - cr * ct / total)


def pack(name, d_lower, d_upper, fins, **kw):
    full = vehicle(d_lower, d_upper, fins, True, **kw)
    bare = vehicle(d_lower, d_upper, fins, False, **kw)
    full["cpWithoutInterstage_m"] = bare["cpRaw_m"]
    full["stabilityWithoutInterstage"] = bare["stabilityCal"]
    full["name"] = name
    full["dLower"] = d_lower
    full["dUpper"] = d_upper
    full["fins"] = fins
    full["propMassKg"] = list(kw.get("prop", PROP))
    full["payloadMassKg"] = kw.get("payload", PAYLOAD)
    full["fairingLengthM"] = kw.get("fairing_length", FAIRING_LENGTH)
    full["finSpanM"] = kw.get("fin_span", FIN_SPAN)
    return full


def main():
    flare_cna, flare_x = conical(4.0, 2.0, length_m(4.0, 2.0), 4.0)
    shoulder_cna, shoulder_x = conical(2.0, 4.0, length_m(2.0, 4.0), 4.0)
    straight = pack("straight", 2.0, 2.0, False)
    flare = pack("flare", 4.0, 2.0, False)
    shoulder = pack("shoulder", 2.0, 4.0, False)
    fin = pack("fin", 2.0, 2.0, True)
    over = pack("overstable", 0.8, 0.8, True, prop=(8000.0, 800.0), payload=20000.0, fairing_length=2.0, fin_span=2.0)
    if abs(fin_cp_aft_of_root_le(1.2, 1.2, 0.0) - 0.25 * 1.2) > 1e-12:
        raise SystemExit("square fin CP is not the quarter chord")
    if not (over["stabilityCal"] > 4.0):
        raise SystemExit("overstable fixture is not above 4 cal: " + str(over["stabilityCal"]))
    if not (flare_cna > 0):
        raise SystemExit("flare CNa is not positive")
    if not (flare["cpRaw_m"] < flare["cpWithoutInterstage_m"]):
        raise SystemExit("flare did not move CP aft")
    if not (shoulder_cna < 0):
        raise SystemExit("shoulder CNa is not negative")
    if not (shoulder["stabilityCal"] < shoulder["stabilityWithoutInterstage"]):
        raise SystemExit("shoulder was not destabilizing")
    if not shoulder["cpAheadOfNose"] or not (shoulder["cpRaw_m"] > shoulder["length_m"]):
        raise SystemExit("shoulder CP should sit ahead of the nose before the marker clamp")
    if shoulder["cp_m"] != shoulder["length_m"]:
        raise SystemExit("shoulder marker was not clamped to the nose")
    doc = {
        "source": "tools/design-v2-literals.py",
        "tolerance": {"massRel": 1e-12, "cpAbsM": 1e-9, "stabilityAbs": 1e-9},
        "interstage": {
            "dLower": 4.0,
            "dUpper": 2.0,
            "material": "al-li",
            "arealKgPerM2": 20.0,
            "lengthM": length_m(4.0, 2.0),
            "lateralAreaM2": lateral_area(4.0, 2.0),
            "massKg": derived_mass("al-li", 4.0, 2.0),
        },
        "drag": {
            "flatCd": 0.02,
            "flareBand": [0.02, 0.05],
            "taperBand": [0.005, 0.01],
            "note": "Flat labelled 0.02 for flare and shoulder. Straight adds 0. A flare is about 0.02–0.05; a shoulder is about 0.005–0.01.",
        },
        "conical": {
            "flare": {
                "note": "Base-wider (dAft 4, dForward 2). CNα > 0, stabilizing, CP moves aft.",
                "dAft": 4.0, "dForward": 2.0, "length": length_m(4.0, 2.0), "dRef": 4.0,
                "cna": flare_cna, "xFromForward": flare_x,
            },
            "shoulder": {
                "note": "Upper-wider (dAft 2, dForward 4). CNα < 0, destabilizing.",
                "dAft": 2.0, "dForward": 4.0, "length": length_m(2.0, 4.0), "dRef": 4.0,
                "cna": shoulder_cna, "xFromForward": shoulder_x,
            },
        },
        "fixture": {
            "propMassKg": list(PROP),
            "engine": "gg-kerolox",
            "engineCount": 1,
            "material": "al-li",
            "safetyFactor": 1.4,
            "separationMassKg": SEPARATION,
            "payloadMassKg": PAYLOAD,
            "fairingLengthM": FAIRING_LENGTH,
            "fairingMaterial": "composite",
            "nose": "ogive",
            "avionics": "standard",
            "finCount": FIN_COUNT,
            "finSpanM": FIN_SPAN,
            "note": "Fairing diameter equals the upper stage. No legs, grid fins, RCS, or strap-ons. interstageMass is omitted so the upper stage carries the derived frustum.",
        },
        "vehicles": {
            "straight": straight,
            "flare": flare,
            "fin": fin,
            "shoulder": shoulder,
            "overstable": over,
        },
    }
    out = Path(__file__).resolve().parents[1] / "data" / "golden" / "design-v2-literals.json"
    out.write_text(json.dumps(doc, indent=2) + "\n", encoding="utf-8")
    print(out)
    print("flare cp", flare["cp_m"], "without", flare["cpWithoutInterstage_m"])
    print("shoulder cal", shoulder["stabilityCal"], "raw cp", shoulder["cpRaw_m"], "marker", shoulder["cp_m"], "length", shoulder["length_m"])
    print("fin cp", fin["cp_m"], "cal", fin["stabilityCal"])
    print("overstable cal", over["stabilityCal"])
    print("straight cal", straight["stabilityCal"], "mass", straight["mass_kg"])


if __name__ == "__main__":
    main()
