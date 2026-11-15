"""Package a complete standalone build and checksum its data, not only the Unity launcher."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil


def sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('build_directory', type=Path)
    parser.add_argument('--verify', action='store_true')
    args = parser.parse_args()
    root = args.build_directory.resolve()
    if args.verify:
        files = json.loads((root / 'file-checksums.json').read_text())
        for entry in files:
            path = root / entry['path']
            if root not in path.resolve().parents or sha256(path) != entry['sha256']:
                raise ValueError('Checksum mismatch: ' + entry['path'])
        print('PACKAGE_VERIFY_PASS', len(files), 'files')
        return
    manifest_path = root / 'build-manifest.json'
    manifest = json.loads(manifest_path.read_text())
    excluded = {'build-manifest.json', 'file-checksums.json'}
    files = [{'path': str(path.relative_to(root)).replace('\\', '/'),
              'bytes': path.stat().st_size, 'sha256': sha256(path)}
             for path in sorted(root.rglob('*')) if path.is_file()
             and str(path.relative_to(root)) not in excluded]
    checksums = json.dumps(files, indent=2) + '\n'
    (root / 'file-checksums.json').write_text(checksums)
    manifest['fileManifest'] = 'file-checksums.json'
    manifest['contentSha256'] = hashlib.sha256(checksums.encode()).hexdigest()
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    archive = Path(shutil.make_archive(str(root), 'zip', root_dir=root.parent, base_dir=root.name))
    archive.with_suffix('.zip.sha256').write_text(sha256(archive) + '  ' + archive.name + '\n')
    print('PACKAGE_PASS', archive, 'files', len(files), 'bytes', archive.stat().st_size)


if __name__ == '__main__':
    main()
