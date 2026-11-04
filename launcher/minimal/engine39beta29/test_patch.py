"""Exercise the version pin, patch and all-or-nothing failure cases on copies."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('native_patch', Path(__file__).with_name('hide_ingame_output_resolution.py'))
patch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(patch)
upstream = Path(sys.argv.pop(1)).resolve()


class PatchChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='native-patch-test-', dir=upstream.parent)
        assert Path(self.temp.name).resolve().is_relative_to(upstream.parent.resolve())
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for name in patch.EXPECTED_SHA256:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(subprocess.check_output(['git', '-C', str(upstream), 'show', f'HEAD:engine/{name}']))

    def contents(self):
        return {name: (self.root / name).read_bytes() for name in patch.EXPECTED_SHA256}

    def test_pinned_engine_and_new_menu_sections(self):
        original = (self.root / 'gpu/shim/engine/option_menu.cpp').read_text(encoding='utf-8')
        patch.apply(self.root)
        menu = (self.root / 'gpu/shim/engine/option_menu.cpp').read_text(encoding='utf-8')
        assets = (self.root / 'tools/engine/menu_assets.cpp').read_text(encoding='utf-8')
        self.assertNotIn('Pick(117004', menu)
        self.assertIn('g_graphics[4].amount = &s.graphics_fog;', menu)
        self.assertIn('{"PCGraphics", 117020, 5, true}', assets)
        # Every new lighting/look row is byte-for-byte the original definition.
        begin, end = 'Setting g_lighting[]', 'constexpr int kGraphicsPick'
        self.assertEqual(original.split(begin)[1].split(end)[0].strip(), menu.split(begin)[1].split('struct Section')[0].strip())
        self.assertIn('if (g_offered[s] && !HiddenSystemPage(s))', menu)
        hidden = menu.split('constexpr bool HiddenSystemPage')[1].split('void* GUEST SystemList')[0]
        for section in ('kControls', 'kKeys', 'kLighting', 'kDetail', 'kLooks'):
            self.assertIn('section == ' + section, hidden)
        for section in ('kGraphics', 'kEffects', 'kEnhance', 'kColour', 'kCamera'):
            self.assertNotIn('section == ' + section, hidden)
        # Offering controls and lighting still follows the same backend switches.
        for line in original.splitlines():
            if 'g_offered[' in line and '=' in line and 'OptionRows::' not in line:
                self.assertIn(line, menu)
        self.assertIn('g_offered[kControls] = g_offered[kKeys] = settings.pc_controls;', menu)
        self.assertIn('g_offered[kLighting] = g_offered[kDetail] = g_offered[kLooks] = Graphics::Enabled();', menu)
        for section in ('PCEnhance', 'PCEffects', 'PCColour', 'PCLighting', 'PCDetail', 'PCLooks'):
            self.assertIn(section, menu)

    def test_changed_second_file_aborts_before_any_write(self):
        path = self.root / 'tools/engine/menu_assets.cpp'
        path.write_bytes(path.read_bytes() + b'\n// changed version\n')
        before = self.contents()
        with self.assertRaises(RuntimeError):
            patch.apply(self.root)
        self.assertEqual(before, self.contents())

    def test_second_application_aborts_without_changes(self):
        patch.apply(self.root)
        before = self.contents()
        with self.assertRaises(RuntimeError):
            patch.apply(self.root)
        self.assertEqual(before, self.contents())


if __name__ == '__main__':
    unittest.main()
