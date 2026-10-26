#!/usr/bin/env bash
set -euo pipefail

version="${1:?Usage: Build-DebianPackage.sh <version> <linux-x64|linux-arm64>}"
runtime_id="${2:?Usage: Build-DebianPackage.sh <version> <linux-x64|linux-arm64>}"
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$ ]]; then
    echo "Invalid semantic version: $version" >&2
    exit 2
fi

case "$runtime_id" in
    linux-x64) runtime_arch=x64; deb_arch=amd64 ;;
    linux-arm64) runtime_arch=arm64; deb_arch=arm64 ;;
    *) echo "Unsupported Linux runtime identifier: $runtime_id" >&2; exit 2 ;;
esac

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
release_directory="$repository_root/release/v$version/linux/$runtime_arch"
application_directory="$release_directory/markdim"
if [[ ! -x "$application_directory/markdim" ]]; then
    echo "The published Linux payload is missing; run Publish-Release.ps1 first." >&2
    exit 1
fi

staging_directory="$(mktemp -d)"
trap 'rm -rf "$staging_directory"' EXIT
package_root="$staging_directory/package"
install_directory="opt/markdim/v$version"
mkdir -p "$package_root/DEBIAN" "$package_root/$install_directory" \
    "$package_root/usr/bin" "$package_root/usr/share/applications" \
    "$package_root/usr/share/icons/hicolor/scalable/apps" "$package_root/usr/share/mime/packages" \
    "$package_root/usr/share/doc/markdim"

cp -a "$application_directory/." "$package_root/$install_directory/"
find "$package_root/$install_directory" -type d -exec chmod 755 {} +
find "$package_root/$install_directory" -type f -exec chmod 644 {} +
chmod 755 "$package_root/$install_directory/markdim"
if [[ -f "$package_root/$install_directory/createdump" ]]; then
    chmod 755 "$package_root/$install_directory/createdump"
fi

cat > "$package_root/usr/bin/markdim" <<EOF
#!/bin/sh
exec "/$install_directory/markdim" "\$@"
EOF
chmod 755 "$package_root/usr/bin/markdim"

sed 's|^Exec=markdim %F$|Exec=/usr/bin/markdim %F|; s|^Icon=markdim$|Icon=markdim|' \
    "$repository_root/packaging/linux/markdim.desktop" \
    > "$package_root/usr/share/applications/markdim.desktop"
install -m 644 "$repository_root/MarkdownDesk/Assets/markdim.svg" \
    "$package_root/usr/share/icons/hicolor/scalable/apps/markdim.svg"
install -m 644 "$repository_root/packaging/linux/org.markdim.Markdown.xml" \
    "$package_root/usr/share/mime/packages/org.markdim.Markdown.xml"
install -m 644 "$repository_root/LICENSE" "$package_root/usr/share/doc/markdim/copyright"
install -m 644 "$repository_root/docs/DEPENDENCIES.md" "$package_root/usr/share/doc/markdim/DEPENDENCIES.md"

cat > "$package_root/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
if [ "$1" = configure ]; then
    command -v update-mime-database >/dev/null 2>&1 && update-mime-database /usr/share/mime >/dev/null 2>&1 || true
    command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database -q /usr/share/applications >/dev/null 2>&1 || true
    command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF
cat > "$package_root/DEBIAN/postrm" <<'EOF'
#!/bin/sh
set -e
if [ "$1" = remove ] || [ "$1" = purge ]; then
    command -v update-mime-database >/dev/null 2>&1 && update-mime-database /usr/share/mime >/dev/null 2>&1 || true
    command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database -q /usr/share/applications >/dev/null 2>&1 || true
    command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF
chmod 755 "$package_root/DEBIAN/postinst" "$package_root/DEBIAN/postrm"

debian_version="$(printf '%s' "$version" | sed 's/-/~/')"
installed_size="$(du -sk "$package_root/opt" "$package_root/usr" | awk '{ total += $1 } END { print total }')"
cat > "$package_root/DEBIAN/control" <<EOF
Package: markdim
Version: $debian_version
Section: editors
Priority: optional
Architecture: $deb_arch
Maintainer: Numera11y <194289234+numera11y@users.noreply.github.com>
Homepage: https://github.com/numera11y/markdim
Installed-Size: $installed_size
Depends: ca-certificates, desktop-file-utils, libc6, libfontconfig1, libfreetype6, libgcc-s1,
 libgl1, libgssapi-krb5-2, libice6, libicu70 | libicu72 | libicu74 | libicu76 | libicu78,
 libsm6, libssl3 | libssl3t64, libstdc++6, libx11-6, libx11-xcb1, libxext6,
 libxkbcommon0, libxkbcommon-x11-0, libxrandr2, libxrender1, shared-mime-info, zlib1g
Description: Markdown editor with live preview
 markdim is a desktop application to read, edit, organize and export
 Markdown documents, with a live preview and multiple document tabs.
EOF

deb_name="markdim-v$version-linux-$deb_arch.deb"
deb_path="$release_directory/$deb_name"
rm -f "$deb_path" "$deb_path.sha256"
dpkg-deb --root-owner-group --build "$package_root" "$deb_path"
(cd "$release_directory" && sha256sum "$deb_name" > "$deb_name.sha256")

echo "Debian package created: $deb_path"
sha256sum "$deb_path"
