@echo off
for /f "delims=" %%a in ('git rev-parse --short HEAD') do @set hash=%%a

python ci\build-scripts\main.py update-gh-list
python ci\build-scripts\main.py app-installer %hash%
pause