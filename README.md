# RelaxHackathon.Compression

> This project was part of a Hackathon and will no longer be maintained. If you think you can extend it feel free to create a fork

This project was created in the Relaxdays Code Challenge Vol. 1. 
See https://sites.google.com/relaxdays.de/hackathon-relaxdays/startseite for more information. 
My participant ID in the challenge was: CC-VOL1-49

## How to run this project

You can get a running version of this code by using:

```bash
git clone https://github.com/Garados007/RelaxHackathon.Compression.git
cd RelaxHackathon.Compression
docker build -t dockerfile-compress .
```

To compress files you need to execute:
```bash
docker run -v $(pwd):/app -it dockerfile-compress compress Dockerfile
```

To uncompress files you need to execute
```bash
docker run -v $(pwd):/app -it dockerfile-compress decompress Dockerfile.compressed
```

Uncompressed files will be stores with a `.decompressed` extension.
