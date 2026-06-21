@echo off
rem Bu betik cift tiklandiginda frontend gelistirme sunucusunu baslatir.
rem Calistigi klasore gecer (betigin bulundugu yer).
cd /d "%~dp0"

rem node_modules yoksa once bagimliliklari kur.
if not exist "node_modules" (
    echo Bagimliliklar kuruluyor, lutfen bekleyin...
    call npm install
    if errorlevel 1 (
        echo.
        echo HATA: npm install basarisiz oldu.
        pause
        exit /b 1
    )
)

echo Gelistirme sunucusu baslatiliyor...
call npm run dev

rem Sunucu kapaninca pencere acik kalsin ki hatalari gorebilesin.
pause
