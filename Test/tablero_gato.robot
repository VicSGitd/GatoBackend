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
    [Documentation]    Los dos michis inician sesión, buscan partida y juegan.
    
   
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
    
    Sleep    5s

    #-------------------------------------
    # Pruebita de Empate
    #-------------------------------------
    
    # Michi1 decide pedir la revancha para el empate
    Switch Browser    Jugador1
    Click Element     id=btnRestart
    
    # Verificamos que el tablero se limpió
    Wait Until Element Contains    id=status    ¡Juego Iniciado!    timeout=5s
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=status    ¡Juego Iniciado!    timeout=5s
    
    # Movimientos para lograr empate
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-4
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-0
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-1
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-7
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-6
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-2
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-5
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-3
    
    Switch Browser    Jugador1
    Wait Until Element Contains    id=turnStatus    Es tu turno...    timeout=10s
    Click Element     id=cell-8
    
    # Checamos el empate
    Wait Until Element Contains    id=turnStatus    ¡EMPATE!    timeout=5s
    
    Switch Browser    Jugador2
    Wait Until Element Contains    id=turnStatus    ¡EMPATE!    timeout=5s
    
    Sleep    5s

    #-------------------------------------
    # Pruebita de ganar por cobardía
    #-------------------------------------
    
    # Michi1 decide pedir la revancha
    Switch Browser    Jugador1
    Click Element     id=btnRestart
    
    # Verificamos que el tablero se limpió para Michi1
    Wait Until Element Contains    id=status    ¡Juego Iniciado!    timeout=5s
    
    # Verificamos que el tablero se limpió para Michi2
    Switch Browser    Jugador2
    Wait Until Element Contains    id=status    ¡Juego Iniciado!    timeout=5s
    
    # Esperamos respuesta
    Sleep    10s
    
    # Michi2 abandona
    Close Window
    

    # Ganando por cobardía
   
    
    # Regresamos a la pantalla de Michi1
    Switch Browser    Jugador1
    
    # Detectamos el abandono
    Handle Alert    action=ACCEPT    timeout=5s
    
    # Verificamos que el sistema protegió a Michi1 y lo regresó al lobby sano y salvo
    Wait Until Element Contains    id=status    Listo para jugar.    timeout=5s
    
    # Pausa final y cerramos lo que quede abierto
    Sleep    3s
    Close All Browsers