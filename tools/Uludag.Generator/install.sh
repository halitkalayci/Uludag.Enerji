#!/usr/bin/env sh
# uludag-gen'i bu bilgisayara global .NET tool olarak kurar; kuruluysa güncel koddan yeniden kurar (spec 0005, R-8).
# Kullanım (repo kökünden):  sh tools/Uludag.Generator/install.sh
# Kaldırma:                  dotnet tool uninstall -g Uludag.Generator
#
# Güvenlik: paket YALNIZCA yerel olarak paketlenen klasörden ve csproj'daki sürümle kurulur (geçici
# nuget.config, <clear/>). Böylece nuget.org'da aynı adla yayımlanmış yabancı bir paket asla kurulmaz.
set -eu

here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../.." && pwd)
out="$root/artifacts/nupkg"
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$here/Uludag.Generator.csproj")

echo "==> paketleniyor: Uludag.Generator $version -> $out"
dotnet pack "$here/Uludag.Generator.csproj" -c Release -o "$out" -nologo -v:q -nodeReuse:false -p:UseSharedCompilation=false

cat > "$out/nuget.config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="uludag-generator-local" value="." />
  </packageSources>
</configuration>
EOF

if dotnet tool list -g | grep -qi '^uludag\.generator '; then
  echo "==> eski sürüm kaldırılıyor"
  dotnet tool uninstall -g Uludag.Generator
fi

echo "==> kuruluyor"
dotnet tool install -g Uludag.Generator --version "$version" --configfile "$out/nuget.config"

echo "==> kuruldu: $(uludag-gen --version 2>/dev/null || echo 'uludag-gen PATH üzerinde değil: ~/.dotnet/tools klasörünü PATH'"'"'e ekleyin')"
