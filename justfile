blend BLEND_FILE:
  python3.11 ./sdf_gen/sdf_gen.py {{BLEND_FILE}}
rgba RGB A RGBA:
  magick {{RGB}} {{A}} -alpha off -compose CopyOpacity -composite {{RGBA}}
rgb R G B RGB:
  magick {{R}} {{G}} {{B}} -combine {{RGB}}
flatc:
  find ./FbSchemas -name '*.fbs' | xargs flatc -o ./assets/scripts --csharp
  find ./FbSchemas -name '*.fbs' | xargs flatc -o ./sdf_gen --python 
ps:
  dotnet-trace ps
trace PROCESS:
  dotnet-trace collect -p {{PROCESS}} -o ./benchmarks/$(date +'%s').nettrace \
  --clreventlevel informational --format Chromium 
convert FN:
  dotnet-trace convert {{FN}} -o ./benchmarks/$(basename {{FN}}).chromium.json --format Chromium 

# Generate glue sources
# bin/godot.linuxbsd.editor.x86_64.mono --headless --generate-mono-glue modules/mono/glue
# Generate binaries
# ./modules/mono/build_scripts/build_assemblies.py --godot-output-dir=./bin --godot-platform=linuxbsd
