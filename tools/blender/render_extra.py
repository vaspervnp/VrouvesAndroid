"""Renders the Android version's own art into build/art/ (shepherd.png, priest.png,
stage10.png ... stage19.png), from inside Blender, as VrouvesPC's art is made:

    import sys
    sys.path[:0] = [r"<this repo>/tools/blender", r"<this repo>/../VrouvesPC/tools/blender"]
    import render_extra; render_extra.main()          # or main(stages=[12])

then tools/import_assets.sh puts it in Vrouves/Assets (the stages scaled to 1920 x 1344, as
JPEG). A stage takes about a minute.
"""
import os

import vrouves_extra as vx

OUT = os.path.join(vx.ROOT, "build", "art")


def main(sprites=None, stages=None):
    done = vx.sprites(OUT, sprites)
    done += ["stage%d" % n for n in vx.stages(OUT, stages)]
    return done
