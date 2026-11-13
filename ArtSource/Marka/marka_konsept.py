# Bilgi Adası logo konseptleri — elle yazılmış SVG üretici (0'dan üret kanıtı)
# Çalıştır: python marka_konsept.py  → konsept/*.svg
import math, os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "konsept")


def svg(body, w=256, h=256):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">{body}</svg>'


def f(x):
    return f"{x:.2f}"


def wave(x0, y, seg, n, sw, color="#fff"):
    """Beyaz dalga şeridi (su çizgisi)."""
    d = f"M{f(x0)} {f(y)} q{seg/2} {-seg*0.4} {seg} 0" + f" t{seg} 0" * (n - 1)
    return f'<path fill="none" stroke="{color}" stroke-width="{sw}" stroke-linecap="round" d="{d}"/>'


# ---------- A: İmbik Ada — damıtma imbiğinin karnında bir ada ----------
def imbik():
    cx, cy, R, r = 112, 164, 70, 56
    wl = 160  # su çizgisi
    dx = math.sqrt(r * r - (wl - cy) ** 2)
    t30 = math.tan(math.radians(30))
    body = (
        f'<circle cx="{cx}" cy="{cy}" r="{R}"/>'
        f'<rect x="{cx-14}" y="66" width="28" height="44"/>'
        f'<rect x="{cx-24}" y="54" width="48" height="16" rx="7"/>'
        # gaga: boyundan 30° aşağı-sağa, sivrilerek
        f'<path d="M{cx+10} 72 L{cx+112} {f(72+102*t30)} L{cx+108} {f(84+98*t30)} L{cx+10} 94 Z"/>'
    )
    # gökyüzü boşluğu; ada sağa kaçık, alçak ve geniş (insan başı okunmasın)
    sky = (
        f'<path fill="#fff" d="M{f(cx-dx)} {wl} A{r} {r} 0 0 1 {f(cx+dx)} {wl} '
        f'L{cx+44} {wl} C{cx+30} {wl-22} {cx-14} {wl-24} {cx-30} {wl} Z"/>'
    )
    # tombul ağaç tepesi: üç kümeli bulut, adanın içine gömülü
    tree = (f'<circle cx="{cx+10}" cy="{wl-24}" r="12"/>'
            f'<circle cx="{cx+24}" cy="{wl-30}" r="14"/>'
            f'<circle cx="{cx+36}" cy="{wl-20}" r="10"/>')
    return svg(f'<g transform="translate(-6 -10)">{body + sky + tree + wave(cx - 40, wl + 20, 20, 4, 7)}</g>')


# ---------- B: Mühür — ustanın mum mührü: ada + köyün 5 görevi ----------
def muhur():
    cx = cy = 128
    R0 = 112
    pts = []
    for i in range(240):
        a = 2 * math.pi * i / 240
        # mum taşması: düşük frekanslı, düzensiz kenar (şişe kapağı tırtığı değil)
        rr = R0 + 4.5 * math.sin(5 * a + 0.4) + 3.0 * math.sin(7 * a + 2.1) + 2.0 * math.sin(11 * a + 1.3)
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    seal = '<path d="M' + " L".join(f"{f(x)} {f(y)}" for x, y in pts) + ' Z"/>'
    ring = f'<circle cx="{cx}" cy="{cy}" r="86" fill="none" stroke="#fff" stroke-width="5"/>'
    wl = 152
    isl = (
        f'<path fill="#fff" d="M{cx-54} {wl} Q{cx} {wl-50} {cx+54} {wl} Z"/>'
        f'<circle fill="#fff" cx="{cx-6}" cy="{wl-30}" r="15"/>'
        f'<circle fill="#fff" cx="{cx+12}" cy="{wl-36}" r="17"/>'
        f'<circle fill="#fff" cx="{cx+28}" cy="{wl-24}" r="12"/>'
        + wave(cx - 44, wl + 20, 22, 4, 6)
    )
    dots = "".join(
        f'<circle fill="#fff" cx="{f(cx+66*math.cos(math.radians(a)))}" cy="{f(cy-4+66*math.sin(math.radians(a)))}" r="5.5"/>'
        for a in (-150, -120, -90, -60, -30)
    )
    return svg(seal + ring + isl + dots)


# ---------- C: Beş Ada — büyüyen beş ada: 5 görev + oyuncunun büyümesi ----------
def bes_ada():
    # aynı su çizgisinde soldan sağa büyüyen beş tombul tepe (kubbe)
    rs = [8, 12, 17, 24, 36]
    gap = 6
    total = sum(2 * r for r in rs) + gap * (len(rs) - 1)
    x = 128 - total / 2
    wl = 140
    body = ""
    for r in rs:
        cx = x + r
        h = r * 1.08  # hafif tombul
        body += f'<path d="M{f(cx-r)} {wl} A{r} {f(h)} 0 0 1 {f(cx+r)} {wl} Z"/>'
        x += 2 * r + gap
    # su: iki siyah dalga
    body += wave(128 - total / 2 + 6, wl + 20, 25, 8, 8, "#000")
    body += wave(128 - total / 2 + 31, wl + 42, 25, 6, 8, "#000")
    return svg(body)


def lockup(sym_svg):
    inner = sym_svg.split(">", 1)[1].rsplit("</svg>", 1)[0]
    txt = (
        '<text x="286" y="158" font-family="Alegreya SC, Georgia, serif" '
        'font-weight="800" font-size="88">Bilgi Adası</text>'
    )
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 860 256" width="860" height="256"><g>{inner}</g>{txt}</svg>'


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for k, s in (("a-imbik", imbik()), ("b-muhur", muhur()), ("c-bes-ada", bes_ada())):
        with open(os.path.join(OUT, f"{k}-v3.svg"), "w", encoding="utf-8") as fh:
            fh.write(s)
        with open(os.path.join(OUT, f"{k}-lockup-v3.svg"), "w", encoding="utf-8") as fh:
            fh.write(lockup(s))
    print("ok")
