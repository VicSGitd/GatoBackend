*** Settings ***
Documentation    Pruebitas automatizadas para el michi.
Library          SeleniumLibrary

*** Variables ***
# Cambia el puerto 4200 por el 5100 si estás levantando el frontend con Blazor en lugar de Angular
${URL_JUEGO}     http://localhost:5287
${NAVEGADOR}     chrome

*** Test Cases ***
Verificar que el jugador puede entrar al tablero
    [Documentation]    Abre el navegador web y entra a la URL local del juego.
    Open Browser       ${URL_JUEGO}    ${NAVEGADOR}
    Maximize Browser Window
    Sleep              3s
    Close Browser