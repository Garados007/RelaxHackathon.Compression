#!/bin/bash

mode=$1
file=$2

case $mode in
	compress)
		newFile="$(basename "$file" .decompressed).compressed"
		/usr/src/RelaxHackathon.Compression/RelaxHackathon.Compression/bin/Release/net5.0/RelaxHackathon.Compression \
			"$file" "/usr/src/comp.temp"
		xz --lzma2 --compress --keep --stdout "/usr/src/comp.temp" > "$newFile"
		;;
	decompress)
		newFile="$(basename "$file" .compressed).decompressed"
		xz --lzma2 --decompress --keep --stdout "${file}" > "$newFile"
		;;
esac
