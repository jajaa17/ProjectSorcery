#!/usr/bin/env python3
"""Render animation previews exported by `Simulator anim <dir> <ids>` into GIFs and contact sheets.

Usage:  python3 preview.py <dir> [--sheet] [--nogif]
Needs Pillow and NumPy (pip install pillow numpy). Draws the solved skeletons exactly as exported: no Unity required.
"""
import json, os, sys, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H, SS = int(os.environ.get("PW", 640)), int(os.environ.get("PH", 360)), 2            # output size, supersampling
PPM = int(os.environ.get("PPM", 78))                          # pixels per metre
GROUND = 0.84                     # ground line height (fraction of image)
BONES = [(0, 1), (1, 2), (2, 3), (3, 4), (2, 13), (13, 5), (7, 8), (8, 14), (14, 5), (5, 15), (15, 9), (9, 10)]


def font(size):
    for p in ["/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"]:
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def load(path):
    with open(path) as f:
        meta = json.loads(f.readline())
        frames = [json.loads(l) for l in f if l.strip()]
    return meta, frames


def render(meta, frames, step=2):
    aura = tuple(int(c * 255) for c in meta["aura"])
    weapon = meta["weapon"]
    out, hist = [], [[], []]
    cam = None
    big, small = font(13 * SS), font(11 * SS)
    for i, fr in enumerate(frames):
        for k, fi in enumerate(fr["f"]):
            segs = fi["sm"] if fi["limb"] == "Hands" else fi["sm"][:1]
            hist[k].append(segs if fi["strike"] and fi["limb"] != "None" else None)
            hist[k] = hist[k][-6:]
        if i % step:
            continue
        me = fr["f"][0]["j"]
        cx = (me[2][0] + fr["f"][1]["j"][2][0]) * 0.5
        cam = cx if cam is None else cam + (cx - cam) * 0.25
        img = Image.new("RGB", (W * SS, H * SS), (10, 10, 18))
        glow = Image.new("RGB", (W * SS, H * SS), (0, 0, 0))
        d, g = ImageDraw.Draw(img, "RGBA"), ImageDraw.Draw(glow)

        def P(p):
            return ((p[0] - cam) * PPM * SS + W * SS / 2, H * SS * GROUND - p[1] * PPM * SS)

        d.line([(0, H * SS * GROUND), (W * SS, H * SS * GROUND)], fill=(60, 60, 80), width=2 * SS)
        for k, fi in enumerate(fr["f"]):
            j = [P(p) for p in fi["j"]]
            col = aura if k == 0 else (160, 170, 200)
            body = (238, 238, 245) if k == 0 else (150, 150, 165)
            # smear ribbons: the swept area of the last few frames
            hs = [h for h in hist[k] if h]
            for s_i in range(2):
                strip = [h[s_i] for h in hist[k] if h and len(h) > s_i]
                for n in range(len(strip) - 1):
                    a0, b0, a1, b1 = strip[n][:2], strip[n][2:], strip[n + 1][:2], strip[n + 1][2:]
                    alpha = int(200 * (n + 1) / len(strip))
                    poly = [P(a0), P(b0), P(b1), P(a1)]
                    g.polygon(poly, fill=tuple(int(c * alpha / 255) for c in col))
                    d.polygon(poly, fill=(255, 255, 255, alpha // 3))
            width = int(9 * SS)
            for a, b in BONES:
                g.line([j[a], j[b]], fill=tuple(int(c * 0.55) for c in col), width=width * 3)
                d.line([j[a], j[b]], fill=body, width=width)
            for p in [j[1], j[3], j[8], j[9], j[2], j[5]]:
                d.ellipse([p[0] - width / 2, p[1] - width / 2, p[0] + width / 2, p[1] + width / 2], fill=body)
            r = 0.25 * PPM * SS * (meta["size"] if k == 0 else meta["dsize"])
            hc = j[6]
            g.ellipse([hc[0] - r * 1.4, hc[1] - r * 1.4, hc[0] + r * 1.4, hc[1] + r * 1.4], outline=tuple(int(c * 0.55) for c in col), width=width * 2)
            d.ellipse([hc[0] - r, hc[1] - r, hc[0] + r, hc[1] + r], outline=body, width=width)
            face = fi["face"]
            d.line([(hc[0] + face * r * 0.35, hc[1] - r * 0.12), (hc[0] + face * r * 0.8, hc[1] - r * 0.05)], fill=col, width=int(5 * SS))
            if k == 0 and weapon not in ("None",):
                base, tip = j[11], j[12]
                if weapon in ("Staff", "Polearm", "Spear", "Broom"):   # long shafts extend behind the hand too
                    base = (base[0] - (tip[0] - base[0]) * 0.8, base[1] - (tip[1] - base[1]) * 0.8)
                d.line([base, tip], fill=(220, 225, 240), width=int(6 * SS))
            if fi["charge"]:
                g.ellipse([j[2][0] - 120 * SS, j[2][1] - 150 * SS, j[2][0] + 120 * SS, j[2][1] + 110 * SS], outline=col, width=6 * SS)
            if fi["contact"] and k == 0:
                p = j[10] if fi["limb"] in ("HandF", "Hands", "None") else j[4] if fi["limb"] == "FootF" else j[0] if fi["limb"] == "FootB" else j[7] if fi["limb"] == "HandB" else j[12]
                g.ellipse([p[0] - 30 * SS, p[1] - 30 * SS, p[0] + 30 * SS, p[1] + 30 * SS], fill=(255, 255, 255))
        glow = glow.filter(ImageFilter.GaussianBlur(9 * SS))
        img = Image.fromarray(np.clip(np.asarray(img, dtype=np.int16) + np.asarray(glow, dtype=np.int16), 0, 255).astype(np.uint8))
        dd = ImageDraw.Draw(img)
        dd.text((14 * SS, 10 * SS), f'{meta["name"]}', font=big, fill=(255, 230, 240))
        dd.text((14 * SS, 30 * SS), f'{fr["label"]}  |  {fr["f"][0]["clip"]}', font=small, fill=(170, 170, 200))
        out.append(img.resize((W, H), Image.LANCZOS))
    return out


def main():
    d = sys.argv[1]
    sheet = "--sheet" in sys.argv
    for path in sorted(glob.glob(os.path.join(d, "*.jsonl"))):
        meta, frames = load(path)
        imgs = render(meta, frames)
        base = os.path.splitext(path)[0]
        if "--nogif" not in sys.argv:
            imgs[0].save(base + ".gif", save_all=True, append_images=imgs[1:], duration=33, loop=0, optimize=False)
        if sheet:
            picks = imgs[::3][:48]
            cols = 6
            rows = (len(picks) + cols - 1) // cols
            sw, sh = W // 2, H // 2
            s = Image.new("RGB", (cols * sw, rows * sh))
            for i, im in enumerate(picks):
                s.paste(im.resize((sw, sh)), ((i % cols) * sw, (i // cols) * sh))
            s.save(base + "_sheet.png")
        print("rendered", base)


if __name__ == "__main__":
    main()
