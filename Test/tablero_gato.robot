*** Settings ***
Documentation    Pruebita del michi
Library          SeleniumLibrary

*** Variables ***
${URL_JUEGO}     http://localhost:5287

*** Keywords ***
Iniciar Sesion
    [Arguments]    ${usuario}    ${password}
    
    # Llenar campos e ir directo a Iniciar Sesión
    Input Text       id=txtUsername    ${usuario}
    Input Password   id=txtPassword    ${password}
    Click Element    xpath=//button[text()='Iniciar Sesión']
    
    Wait Until Element Is Visible    id=gameZone    timeout=10s

*** Test Cases ***
Partida completa entre los dos botcitos
    [Documentation]    Dos jugadores inician sesión, buscan partida y juegan.
    
   
    # Pausa
    Set Selenium Speed    0.8s
    
   
    # Inicia Bot1
  
    Open Browser    ${URL_JUEGO}    chrome    alias=Jugador1
    Set Window Position    0      0
    Set Window Size        960    1080
    
    
    Iniciar Sesion    Michi1    gato
    
    Click Element    id=btnBuscar
    Wait Until Element Contains    id=status    Esperando a otro jugador...    timeout=10s
    
   
    # Inicia Bot2
    
    Open Browser    ${URL_JUEGO}    chrome    alias=Jugador2
    Set Window Position    960    0
    Set Window Size        960    1080
    
    
    Iniciar Sesion    Michi2    gato
    
    Click Element    id=btnBuscar
    Wait Until Element Contains    id=status    ¡Juego Iniciado!    timeout=10s
    
    
    # Inicio de los golpes bot 1 vs bot 2
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=status    ¡Juego Iniciado!    timeout=10s
    Click Element     id=cell-0
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-3
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-1
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-4
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-2
    
    
    # Checamos quién ganó
    
    Wait Until Element Contains    id=turnStatus    ¡GANASTE!    timeout=5s
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    ¡PERDISTE!    timeout=5s
    
    Sleep    10s
    Close All Browsers