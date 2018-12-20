#!/bin/sh

DIR=$( cd "$( dirname "$0" )" && pwd )

cd $DIR

./glslangvalidator -V -S vert Model.vert -o Model.vert.spv
./glslangvalidator -V -S frag Model.frag -o Model.frag.spv
./glslangvalidator -V -S vert Output.vert -o Output.vert.spv
./glslangvalidator -V -S frag Output.frag -o Output.frag.spv
./glslangvalidator -V -S vert Skybox.vert -o Skybox.vert.spv
./glslangvalidator -V -S frag Skybox.frag -o Skybox.frag.spv
./glslangvalidator -V -S vert Sao.vert -o Sao.vert.spv
./glslangvalidator -V -S frag Sao.frag -o Sao.frag.spv
./glslangvalidator -V -S vert DepthLimitedBlur.vert -o DepthLimitedBlur.vert.spv
./glslangvalidator -V -S frag DepthLimitedBlur.frag -o DepthLimitedBlur.frag.spv
./glslangvalidator -V -S vert DepthNormal.vert -o DepthNormal.vert.spv
./glslangvalidator -V -S frag DepthNormal.frag -o DepthNormal.frag.spv