#!/bin/sh

DIR=$( cd "$( dirname "$0" )" && pwd )

cd $DIR

./glslangvalidator -V -S vert Model.vert -o Model.vert.spv
./glslangvalidator -V -S frag Model.frag -o Model.frag.spv
./glslangvalidator -V -S vert Output.vert -o Output.vert.spv
./glslangvalidator -V -S frag Output.frag -o Output.frag.spv
./glslangvalidator -V -S vert Sao.vert -o Sao.vert.spv
./glslangvalidator -V -S frag Sao.frag -o Sao.frag.spv
./glslangvalidator -V -S vert DepthLimitedBlur.vert -o DepthLimitedBlur.vert.spv
./glslangvalidator -V -S frag DepthLimitedBlur.frag -o DepthLimitedBlur.frag.spv
./glslangvalidator -V -S vert DepthNormal.vert -o DepthNormal.vert.spv
./glslangvalidator -V -S frag DepthNormal.frag -o DepthNormal.frag.spv

./spirv-cross --fixup-clipspace --version 100 Model.vert.spv --output comp/Model.glsl.vert
./spirv-cross --fixup-clipspace --version 100 Model.frag.spv --output comp/Model.glsl.frag
./spirv-cross --fixup-clipspace --version 100 Output.vert.spv --output comp/Output.glsl.vert
./spirv-cross --fixup-clipspace --version 100 Output.frag.spv --output comp/Output.glsl.frag
./spirv-cross --fixup-clipspace --version 100 Sao.vert.spv --output comp/Sao.glsl.vert
./spirv-cross --fixup-clipspace --version 100 Sao.frag.spv --output comp/Sao.glsl.frag
./spirv-cross --fixup-clipspace --flip-vert-y --version 100 DepthLimitedBlur.vert.spv --output comp/DepthLimitedBlur.glsl.vert
./spirv-cross --fixup-clipspace --flip-vert-y --version 100 DepthLimitedBlur.frag.spv --output comp/DepthLimitedBlur.glsl.frag
./spirv-cross --fixup-clipspace --version 100 DepthNormal.vert.spv --output comp/DepthNormal.glsl.vert
./spirv-cross --fixup-clipspace --version 100 DepthNormal.frag.spv --output comp/DepthNormal.glsl.frag
