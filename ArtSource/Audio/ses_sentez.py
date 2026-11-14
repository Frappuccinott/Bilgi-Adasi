# -*- coding: utf-8 -*-
"""
Bilgi Adası — tüm oyun seslerini sıfırdan sentezler (örnek/kayıt kullanılmaz, telif yok).
Çalıştır:  python ArtSource/Audio/ses_sentez.py
Çıktı:     Assets/_Project/Audio/{SFX,UI,Ambience,Music}/*.wav  (44.1 kHz, 16 bit)
Yapay zekâ beyanı: bu betik Claude (Anthropic) ile yazıldı; LICENSES.md'de kayıtlı.
"""
import os
import numpy as np
from scipy import signal
from scipy.io import wavfile

SR = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Project", "Audio"))
rng = np.random.default_rng(14112026)


# ---------------------------------------------------------------- yardımcılar
def t_axis(dur):
    return np.arange(int(dur * SR)) / SR


def silence(dur):
    return np.zeros(int(dur * SR))


def noise(dur):
    return rng.standard_normal(int(dur * SR))


def _sos(kind, f, order=2):
    return signal.butter(order, f, btype=kind, fs=SR, output="sos")


def lp(x, f, order=2):
    return signal.sosfilt(_sos("lowpass", f, order), x)


def hp(x, f, order=2):
    return signal.sosfilt(_sos("highpass", f, order), x)


def bp(x, lo, hi, order=2):
    hi = min(hi, SR / 2 * 0.95)
    lo = min(lo, hi * 0.8)
    return signal.sosfilt(_sos("bandpass", [lo, hi], order), x)


def reson(x, f, q):
    b, a = signal.iirpeak(f, q, fs=SR)
    return signal.lfilter(b, a, x)


def expdec(n, tau):
    return np.exp(-np.arange(n) / SR / tau)


def adsr(n, a=0.005, r=0.05):
    e = np.ones(n)
    na, nr = max(1, int(a * SR)), max(1, int(r * SR))
    na, nr = min(na, n // 2), min(nr, n // 2)
    e[:na] = np.linspace(0, 1, na)
    e[n - nr:] *= np.linspace(1, 0, nr)
    return e


def mix_at(dst, src, start_s, gain=1.0):
    i = int(start_s * SR)
    if i >= len(dst):
        return dst
    j = min(len(dst), i + len(src))
    dst[i:j] += src[: j - i] * gain
    return dst


def S(*xs):
    """Farklı uzunluktaki sinyalleri sıfırla doldurup toplar."""
    n = max(len(x) for x in xs)
    out = np.zeros(n)
    for x in xs:
        out[: len(x)] += x
    return out


def normalize(x, peak_db=-1.0):
    m = np.max(np.abs(x))
    return x if m < 1e-9 else x / m * (10 ** (peak_db / 20))


def fade(x, fin=0.002, fout=0.01):
    x = x.copy()
    a, b = int(fin * SR), int(fout * SR)
    if a > 0:
        x[:a] *= np.linspace(0, 1, a)
    if b > 0:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def loopify(x, xf=0.5):
    """Sonu başa çapraz geçişle karıştırır: kesintisiz döngü."""
    n = int(xf * SR)
    body = x[: len(x) - n].copy()
    w = np.linspace(0, 1, n)
    body[:n] = body[:n] * np.sqrt(w) + x[len(x) - n:] * np.sqrt(1 - w)
    return body


def write(folder, name, x, peak_db=-1.0, loop=False):
    os.makedirs(os.path.join(ROOT, folder), exist_ok=True)
    x = np.asarray(x, dtype=np.float64)
    if x.ndim == 1:
        x = normalize(x, peak_db) if loop else fade(normalize(x, peak_db))
    else:
        m = np.max(np.abs(x))
        x = x / m * (10 ** (peak_db / 20))
    path = os.path.join(ROOT, folder, name + ".wav")
    wavfile.write(path, SR, (np.clip(x, -1, 1) * 32767).astype(np.int16))
    print("  ", folder + "/" + name + ".wav", f"{x.shape[0] / SR:.2f} sn")


# ---------------------------------------------------------------- temel sesler
def bubble(f0, dur=0.06, rise=None):
    """Sıvı kabarcığı: frekansı yükselen, hızla sönen sinüs (Minnaert)."""
    t = t_axis(dur)
    rise = rise if rise is not None else rng.uniform(4, 9)
    ph = 2 * np.pi * np.cumsum(f0 * (1 + rise * t)) / SR
    return np.sin(ph) * np.exp(-t / (dur * 0.35)) * adsr(len(t), 0.001, 0.005)


def bubbles(dur, rate, fmin, fmax, gain=1.0, size=(0.03, 0.09)):
    out = silence(dur)
    for _ in range(int(dur * rate)):
        f = np.exp(rng.uniform(np.log(fmin), np.log(fmax)))
        b = bubble(f, rng.uniform(*size))
        mix_at(out, b * rng.uniform(0.3, 1.0) * gain, rng.uniform(0, dur))
    return out


def glass(freq, dur=0.6, bright=1.0):
    """Cam/kavanoz tınısı: inharmonik kısmi tonlar."""
    t = t_axis(dur)
    out = np.zeros_like(t)
    for ratio, amp, tau in [(1, 1, 0.35), (2.32, 0.5, 0.2), (4.25, 0.35 * bright, 0.12), (6.63, 0.2 * bright, 0.07)]:
        out += amp * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 6)) * np.exp(-t / (tau * dur / 0.6))
    return out * adsr(len(t), 0.001, 0.02)


def bell(freq, dur=2.0, amp=1.0):
    t = t_axis(dur)
    out = np.zeros_like(t)
    for ratio, a, tau in [(0.5, 0.35, 1.2), (1, 1, 0.9), (1.19, 0.3, 0.5), (1.5, 0.25, 0.5), (2.0, 0.4, 0.35), (2.74, 0.2, 0.2), (3.76, 0.1, 0.12)]:
        out += a * np.sin(2 * np.pi * freq * ratio * t) * np.exp(-t / (tau * dur / 2))
    return out * adsr(len(t), 0.002, 0.05) * amp


def coin(freq):
    t = t_axis(0.35)
    out = np.zeros_like(t)
    for ratio, a in [(1, 1), (1.47, 0.6), (2.09, 0.5), (2.56, 0.4), (3.9, 0.25)]:
        out += a * np.sin(2 * np.pi * freq * ratio * t + rng.uniform(0, 6))
    return out * np.exp(-t / 0.08) * adsr(len(t), 0.0005, 0.01)


def karplus(freq, dur, decay=0.996, bright=0.5, amp=1.0):
    """Telli çalgı (lavta) — Karplus-Strong, lfilter ile hızlı."""
    n = int(dur * SR)
    N = max(2, int(SR / freq))
    exc = np.zeros(n)
    burst = rng.uniform(-1, 1, N)
    burst = lp(burst, 800 + 6000 * bright, 1)
    exc[:N] = burst
    a = np.zeros(N + 2)
    a[0] = 1.0
    a[N] = -0.5 * decay
    a[N + 1] = -0.5 * decay
    y = signal.lfilter([1.0], a, exc)
    # gövde rezonansı
    body = reson(y, 220, 3) * 0.4 + reson(y, 520, 4) * 0.25
    return (y + body) * adsr(n, 0.001, 0.08) * amp


def recorder(freq, dur, amp=1.0):
    """Blok flüt: hafif vibratolu sinüs + harmonik + nefes gürültüsü."""
    t = t_axis(dur)
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t / 0.3, 0, 1)
    ph = 2 * np.pi * np.cumsum(freq * vib) / SR
    tone = np.sin(ph) + 0.18 * np.sin(2 * ph) + 0.08 * np.sin(3 * ph)
    breath = bp(noise(dur), freq * 1.5, min(freq * 6, 16000)) * 0.04
    return (tone + breath) * adsr(len(t), 0.04, min(0.12, dur * 0.4)) * amp


def knock(f=180, dur=0.12, amp=1.0):
    t = t_axis(dur)
    s = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.025)
    s += bp(noise(dur), f * 2, f * 8) * np.exp(-t / 0.01) * 0.6
    return s * amp


def paper(dur, density=60, lo=1500, hi=9000):
    """Kâğıt hışırtısı: kümelenmiş kısa gürültü patlamaları."""
    out = silence(dur)
    env = np.sin(np.pi * np.clip(t_axis(dur) / dur, 0, 1)) ** 0.7
    for _ in range(int(dur * density)):
        g = rng.uniform(0.004, 0.03)
        b = noise(g) * np.hanning(int(g * SR))
        mix_at(out, b * rng.uniform(0.2, 1), rng.uniform(0, dur))
    return bp(out, lo, hi) * env


def whoosh(dur, f0, f1, q=2.0):
    n = int(dur * SR)
    x = noise(dur)
    out = np.zeros(n)
    seg = 512
    fs = np.geomspace(f0, f1, n // seg + 1)
    zi = None
    for i, f in enumerate(fs):
        a, b = i * seg, min(n, (i + 1) * seg)
        if a >= n:
            break
        sos = _sos("bandpass", [f / (1 + 1 / q), min(f * (1 + 1 / q), SR / 2 - 100)])
        if zi is None:
            zi = signal.sosfilt_zi(sos) * 0
        y, zi = signal.sosfilt(sos, x[a:b], zi=zi)
        out[a:b] = y
    return out * np.sin(np.pi * np.linspace(0, 1, n)) ** 1.5


def crackle(dur, rate):
    out = silence(dur)
    for _ in range(int(dur * rate)):
        g = rng.uniform(0.001, 0.006)
        b = noise(g) * expdec(int(g * SR), g / 3)
        mix_at(out, b * rng.uniform(0.1, 1) ** 2, rng.uniform(0, dur))
    return hp(out, 1200)


# ---------------------------------------------------------------- SFX
def make_sfx():
    print("SFX")
    # Kavanozdan kazana dökme (3 çeşit)
    for k in range(3):
        d = 0.75
        x = bp(noise(d), 600, 5000) * np.exp(-t_axis(d) / 0.25) * 0.35
        x[: int(0.03 * SR)] *= np.linspace(0, 1, int(0.03 * SR))
        x += bubbles(d, 45, 250, 900, 0.9)
        x += lp(noise(d), 300) * np.exp(-t_axis(d) / 0.15) * 0.6
        write("SFX", f"Kazan_Dok_{k + 1}", x, -3)

    # Boşaltma: gurul gurul, giderek alçalan
    d = 1.8
    x = silence(d)
    for i in range(160):
        p = rng.uniform(0, d)
        f = 500 * (1 - 0.6 * p / d) * rng.uniform(0.7, 1.4)
        mix_at(x, bubble(f, rng.uniform(0.04, 0.12), rng.uniform(2, 5)), p, rng.uniform(0.3, 1))
    x += lp(noise(d), 250) * np.linspace(1, 0.2, len(x)) * 0.8
    write("SFX", "Kazan_Bosalt", x, -3)

    # Ocak yakma: fuşş + çıtırtı
    d = 1.1
    x = whoosh(d, 300, 2500, 1.5) * 1.2 + lp(noise(d), 180) * np.sin(np.pi * np.linspace(0, 1, int(d * SR))) * 0.8
    x += crackle(d, 40) * 0.4
    write("SFX", "Ocak_Yak", x, -3)

    # Kaynama döngüsü (tepkime süresince)
    d = 5.0
    x = bubbles(d + 0.5, 70, 120, 600, 1.0, (0.04, 0.14)) + lp(noise(d + 0.5), 140) * 0.5
    write("SFX", "Kazan_Kaynama_Dongu", loopify(x, 0.5), -4, loop=True)

    # Başarılı tepkime: parlayan cam ışıltısı (pentatonik yükseliş)
    d = 2.2
    x = silence(d)
    notes = [587.3, 659.3, 784.0, 880.0, 1174.7, 1318.5]
    for i, f in enumerate(notes):
        mix_at(x, glass(f, 1.2, 0.7), 0.06 + i * 0.075, 0.8)
    mix_at(x, bell(587.3, 2.0, 0.6), 0.0)
    x += bp(noise(d), 5000, 12000) * np.exp(-t_axis(d) / 0.6) * 0.05
    write("SFX", "Tepkime_Basarili", x, -2)

    # Köpürme (hatalı): fokurtu yükselir, boğuk "pof", inen ton (hafif komik)
    d = 1.8
    x = silence(d)
    for i in range(140):
        p = rng.uniform(0, 1.0)
        mix_at(x, bubble(rng.uniform(200, 700) * (1 + p), rng.uniform(0.03, 0.08)), p, 0.3 + 0.7 * p)
    pof = lp(noise(0.4), 400) * np.exp(-t_axis(0.4) / 0.08) * 2.0
    pof += np.sin(2 * np.pi * 70 * t_axis(0.4)) * np.exp(-t_axis(0.4) / 0.1)
    mix_at(x, pof, 1.0)
    t = t_axis(0.7)
    slide = np.sin(2 * np.pi * np.cumsum(np.linspace(420, 260, len(t))) / SR) * np.exp(-t / 0.3) * 0.25
    mix_at(x, slide, 1.05)
    write("SFX", "Kazan_Kopurme", x, -3)

    # Kavanoz şıngırtısı (raf al/koy)
    for k, f in enumerate([1850, 2100, 2350]):
        x = S(glass(f, 0.45), glass(f * 1.07, 0.3) * 0.3, knock(260, 0.08, 0.5))
        write("SFX", f"Kavanoz_{k + 1}", x, -4)

    # Akçe (teslim, satın alma)
    d = 0.9
    x = silence(d)
    for i in range(7):
        mix_at(x, coin(rng.uniform(2600, 3600)), 0.02 + i * rng.uniform(0.05, 0.09), rng.uniform(0.4, 1))
    write("SFX", "Akce", x, -3)
    x = silence(0.4)
    mix_at(x, coin(3100), 0.0)
    mix_at(x, coin(3500), 0.07, 0.6)
    write("SFX", "Akce_Tek", x, -4)

    # Kapı gıcırtısı (stick-slip) — aç / kapat
    def creak(dur, rate0, rate1):
        n = int(dur * SR)
        rate = np.geomspace(rate0, rate1, n) * (1 + 0.25 * np.cumsum(rng.standard_normal(n)) / np.sqrt(n) * 3)
        rate = np.clip(rate, 15, 400)
        ph = np.cumsum(rate / SR)
        imp = np.diff(np.floor(ph), prepend=0) * rng.uniform(0.5, 1, n)
        y = reson(imp, 650, 12) + reson(imp, 1380, 15) * 0.6 + reson(imp, 2700, 18) * 0.3
        return y * np.sin(np.pi * np.linspace(0, 1, n)) ** 0.5

    x = creak(0.9, 40, 110) + lp(noise(0.9), 120) * 0.05
    write("SFX", "Kapi_Ac", x, -4)
    x = creak(0.55, 120, 50)
    tail = silence(0.35)
    x = np.concatenate([x, tail])
    mix_at(x, knock(95, 0.3, 2.5) + lp(noise(0.3), 300) * np.exp(-t_axis(0.3) / 0.05), 0.55)
    write("SFX", "Kapi_Kapat", x, -3)

    # Kilitli dolap sarsılması
    d = 0.5
    x = silence(d)
    for i in range(5):
        mix_at(x, knock(rng.uniform(200, 320), 0.1), i * 0.07 + rng.uniform(0, 0.02), rng.uniform(0.5, 1))
    mix_at(x, coin(1900) * 0.3, 0.05)
    write("SFX", "Dolap_Kilitli", x, -4)

    # Ayak sesleri (toprak/taş, 6 çeşit), zıplama, iniş
    for k in range(6):
        d = 0.22
        t = t_axis(d)
        thump = np.sin(2 * np.pi * rng.uniform(70, 95) * t) * np.exp(-t / 0.03)
        scuff = bp(noise(d), 300, 2500) * np.exp(-t / 0.02) * 0.8
        grit = silence(d)
        for _ in range(25):
            g = 0.002
            mix_at(grit, noise(g) * np.hanning(int(g * SR)), rng.uniform(0.005, 0.09), rng.uniform(0.1, 0.6))
        x = thump + scuff + bp(grit, 2500, 8000) * 0.5
        write("SFX", f"Adim_{k + 1}", x, -6)
    write("SFX", "Zipla", whoosh(0.25, 400, 1600, 1.2) * 0.7 + paper(0.25, 40, 800, 4000) * 0.4, -8)
    d = 0.35
    t = t_axis(d)
    x = np.sin(2 * np.pi * 60 * t) * np.exp(-t / 0.06) + bp(noise(d), 200, 3000) * np.exp(-t / 0.04)
    write("SFX", "Inis", x, -4)

    # Bisiklet: tekerlek + zincir döngüsü, zil
    d = 3.0
    x = lp(noise(d + 0.3), 400) * 0.6 + bp(noise(d + 0.3), 2000, 6000) * 0.05
    for i in range(int((d + 0.3) * 9)):
        mix_at(x, knock(rng.uniform(2500, 3500), 0.015, 0.15), i / 9 + rng.uniform(0, 0.01))
    write("SFX", "Bisiklet_Tekerlek_Dongu", loopify(x, 0.3), -6, loop=True)
    x = silence(1.4)
    for p in (0.0, 0.14):
        mix_at(x, S(bell(2350, 1.2, 1.0) * 0.6, glass(3300, 0.8) * 0.3), p)
    write("SFX", "Bisiklet_Zil", x, -4)


# ---------------------------------------------------------------- UI
def make_ui():
    print("UI")
    write("UI", "Ui_Gezin", S(knock(1400, 0.05, 1.0) * 0.6, glass(2800, 0.08) * 0.2), -12)
    x = silence(0.3)
    mix_at(x, karplus(659.3, 0.3, 0.99, 0.6), 0)
    mix_at(x, karplus(987.8, 0.25, 0.99, 0.6), 0.06)
    write("UI", "Ui_Onay", x, -8)
    x = silence(0.3)
    mix_at(x, karplus(659.3, 0.3, 0.99, 0.4), 0)
    mix_at(x, karplus(493.9, 0.25, 0.99, 0.4), 0.06)
    write("UI", "Ui_Geri", x, -9)
    write("UI", "Ui_Slot", S(knock(2200, 0.04) * 0.5, glass(3500, 0.06) * 0.15), -14)
    write("UI", "Sayfa_Cevir", paper(0.35, 120) + whoosh(0.35, 800, 3000) * 0.3, -8)
    x = paper(0.5, 90)
    mix_at(x, knock(130, 0.2, 1.2) + lp(noise(0.2), 600) * np.exp(-t_axis(0.2) / 0.03), 0.36)
    write("UI", "Defter_Ac", x, -6)
    write("UI", "Defter_Kapat", S(knock(150, 0.15, 1.0), paper(0.2, 60) * 0.5), -7)
    write("UI", "Harita_Ac", paper(0.8, 70, 900, 7000) + whoosh(0.8, 300, 1500) * 0.3, -6)
    write("UI", "Diyalog_Ilerle", S(paper(0.12, 80, 2000, 8000) * 0.6, knock(900, 0.05) * 0.3), -14)
    write("UI", "Duraklat", whoosh(0.35, 1800, 400, 1.5), -10)

    # Doğru / yanlış / bilgi (masa görevi)
    x = silence(1.0)
    mix_at(x, glass(1046.5, 0.9, 0.6), 0)
    mix_at(x, glass(1568.0, 0.9, 0.6), 0.09)
    write("UI", "Dogru", x, -5)
    x = silence(0.6)
    mix_at(x, karplus(220.0, 0.5, 0.985, 0.2), 0)
    mix_at(x, karplus(207.7, 0.5, 0.985, 0.2), 0.12)
    mix_at(x, knock(160, 0.15, 0.6), 0)
    write("UI", "Yanlis", x, -7)
    write("UI", "Bilgi", glass(1318.5, 0.5, 0.4), -9)

    # Başarım: lavta arpejiyle küçük müjde
    x = silence(2.2)
    for i, f in enumerate([293.7, 370.0, 440.0, 587.3]):
        mix_at(x, karplus(f, 1.6, 0.997, 0.6), i * 0.09)
    mix_at(x, bell(1174.7, 1.8, 0.5), 0.3)
    write("UI", "Basarim", x, -3)
    # Köy tamamlandı: iki akorlu daha büyük müjde
    x = silence(4.0)
    for i, f in enumerate([196.0, 246.9, 293.7, 392.0]):
        mix_at(x, karplus(f, 2.0, 0.997, 0.6), i * 0.08)
    for i, f in enumerate([293.7, 370.0, 440.0, 587.3, 740.0]):
        mix_at(x, karplus(f, 2.8, 0.998, 0.65), 0.75 + i * 0.07)
    mix_at(x, bell(587.3, 3.0, 0.6), 0.75)
    mix_at(x, bell(880.0, 2.6, 0.35), 0.95)
    write("UI", "Koy_Tamamlandi", x, -2)


# ---------------------------------------------------------------- ortam
def make_ambience():
    print("Ambience")
    d = 40.0
    n = int(d * SR)
    brown = np.cumsum(rng.standard_normal(n + SR))
    brown = hp(brown, 25)
    wind = lp(brown, 500)
    t = np.arange(len(wind)) / SR
    lfo = 0.6 + 0.4 * np.sin(2 * np.pi * t / 11.0) * np.sin(2 * np.pi * t / 4.3 + 1)
    wind = wind / np.max(np.abs(wind)) * lfo * 0.35
    whistle = bp(rng.standard_normal(len(wind)), 700, 900) * (0.5 + 0.5 * np.sin(2 * np.pi * t / 7.0)) * 0.01
    x = wind + whistle
    # kuşlar
    for _ in range(26):
        start = rng.uniform(0, d)
        base = rng.uniform(2600, 4200)
        for c in range(rng.integers(2, 6)):
            cd = rng.uniform(0.06, 0.14)
            tt = t_axis(cd)
            f = base * (1 + rng.uniform(-0.15, 0.35) * tt / cd) * (1 + 0.04 * np.sin(2 * np.pi * 45 * tt))
            ch = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * tt / cd) ** 2
            mix_at(x, ch * rng.uniform(0.02, 0.06), start + c * rng.uniform(0.12, 0.2))
    write("Ambience", "Sehir_Ortam_Dongu", loopify(x, 1.0), -6, loop=True)

    # Atölye ocağı çıtırtısı
    d = 12.0
    x = lp(noise(d + 0.8), 160) * 0.5 + crackle(d + 0.8, 25) * 0.7
    roar = bp(noise(d + 0.8), 200, 900) * (0.6 + 0.4 * np.sin(2 * np.pi * t_axis(d + 0.8) / 3.1)) * 0.15
    write("Ambience", "Ocak_Citirti_Dongu", loopify(x + roar, 0.8), -6, loop=True)


# ---------------------------------------------------------------- müzik
def make_music():
    print("Music")
    bpm = 96
    beat = 60 / bpm
    bar = 3 * beat
    D4 = 293.66
    scale = [0, 2, 3, 5, 7, 9, 10, 12, 14, 15, 17]  # Re dorian (D E F G A B C D E F G)

    def deg(d, octave=0):
        return D4 * 2 ** ((scale[d] + 12 * octave) / 12)

    melody = [
        [(4, 1), (7, 1), (6, 1)], [(7, 2), (4, 1)], [(5, 1), (6, 1), (7, 1)], [(8, 2), (7, 1)],
        [(6, 1), (5, 1), (4, 1)], [(3, 1), (4, 1), (2, 1)], [(1, 1), (2, 1), (1, 1)], [(0, 3)],
        [(2, 1), (4, 1), (6, 1)], [(7, 2), (6, 1)], [(6, 1), (4, 1), (3, 1)], [(4, 3)],
        [(7, 1), (8, 1), (9, 1)], [(8, 1), (7, 1), (6, 1)], [(4, 1), (3, 1), (1, 1)], [(0, 3)],
    ]
    # akor kökü (Re=0 derecesi) ve akor dereceleri
    chords = [0, 0, 3, 6, 4, 2, 6, 0, 2, 0, 6, 4, 0, 6, 4, 0]
    triad = {0: (0, 2, 4), 2: (2, 4, 6), 3: (3, 5, 7), 4: (4, 6, 8), 6: (6, 8, 10)}

    bars = len(melody) * 2
    dur = bars * bar
    tail = 3.0
    L = np.zeros(int((dur + tail) * SR))
    R = np.zeros_like(L)

    for b in range(bars):
        t0 = b * bar
        root = chords[b % 16]
        tri = triad[root]
        # lavta eşliği: bas + iki akor notası
        bass = deg(tri[0], -2 if tri[0] < 5 else -2)
        mix_at(L, karplus(bass, bar * 1.2, 0.997, 0.35, 0.55), t0)
        mix_at(R, karplus(bass, bar * 1.2, 0.997, 0.35, 0.45), t0)
        for k, d in enumerate((tri[1], tri[2])):
            f = deg(d, -1)
            s = karplus(f, beat * 1.6, 0.995, 0.5, 0.3)
            mix_at(L, s, t0 + (k + 1) * beat + rng.uniform(0, 0.01), 0.9)
            mix_at(R, s, t0 + (k + 1) * beat + rng.uniform(0, 0.01), 0.6)
        # melodi: 1. tur blok flüt, 2. tur lavta
        pos = t0
        for d, beats in melody[b % 16]:
            nd = beats * beat
            if b < 16:
                s = recorder(deg(d), nd * 0.95, 0.32)
                mix_at(L, s, pos, 0.6)
                mix_at(R, s, pos, 0.9)
            else:
                s = karplus(deg(d), nd + 0.8, 0.998, 0.7, 0.7)
                mix_at(L, s, pos, 0.75)
                mix_at(R, s, pos, 0.9)
            pos += nd
        # hafif tef: 1. vuruşta
        if b % 2 == 0:
            tt = t_axis(0.25)
            tam = bp(noise(0.25), 3000, 9000) * np.exp(-tt / 0.06) * 0.06 + np.sin(2 * np.pi * 110 * tt) * np.exp(-tt / 0.05) * 0.08
            mix_at(L, tam, t0, 0.8)
            mix_at(R, tam, t0, 0.8)

    # kesintisiz döngü: kuyruk başa eklenir
    n = int(dur * SR)
    for ch in (L, R):
        ch[: len(ch) - n] += ch[n:]
    L, R = L[:n], R[:n]
    # yumuşak oda yankısı
    for ch in (L, R):
        for dly, g in [(0.031, 0.25), (0.047, 0.2), (0.071, 0.15), (0.113, 0.1)]:
            k = int(dly * SR)
            ch[k:] += ch[:-k] * g
    st = np.stack([L, R], axis=1)
    write("Music", "Muzik_Ana_Dongu", st, -3)


if __name__ == "__main__":
    print("Hedef:", ROOT)
    make_sfx()
    make_ui()
    make_ambience()
    make_music()
    print("Bitti.")
