#!/bin/bash

mode=$1
file=$2

case $mode in
	compress)
		newFile="$(basename "$file" .decompressed).compressed"
		RelaxHackathon.Compression/bin/Release/net5.0/RelaxHackathon.Compression "$file" "${newFile}.temp"
		xz --format=lzma2 --compress --keep --stdout "${newFile}.temp" > "$newFile"
		rm "${newFile}.temp"
		;;
	decompress)
		newFile="$(basename "$file" .compressed).decompressed"
		xz --format=lzma2 --decompress --keep --stdout "${file}" > "$newFile"
		;;
esac
