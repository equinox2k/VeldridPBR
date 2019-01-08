#!/bin/sh

DIR=$( cd "$( dirname "$0" )" && pwd )

cd $DIR

./glslangvalidator -V -S vert Model.vert -o Model.vert.spv
./glslangvalidator -V -S frag Model.frag -o Model.frag.spv
./glslangvalidator -V -S vert Output.vert -o Output.vert.spv
./glslangvalidator -V -S frag Output.frag -o Output.frag.spv

./spirv-cross --fixup-clipspace --version 100 Model.vert.spv --output comp/Model.glsl.vert
./spirv-cross --fixup-clipspace --version 100 Model.frag.spv --output comp/Model.glsl.frag
./spirv-cross --fixup-clipspace --version 100 Output.vert.spv --output comp/Output.glsl.vert
./spirv-cross --fixup-clipspace --version 100 Output.frag.spv --output comp/Output.glsl.frag

