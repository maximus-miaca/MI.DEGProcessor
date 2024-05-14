#!/bin/sh

echo "try ls"
ls /var/scratch
echo "end ls"

echo "try list"
klist
echo "end list"

kinit -c FILE:/var/scratch/krbcache

dotnet MI.DEGService.dll

echo "AFTER DLL"