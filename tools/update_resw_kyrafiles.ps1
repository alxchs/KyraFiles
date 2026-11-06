# Copyright (c) Files Community
# SPDX-License-Identifier: MPL-2.0

# 1. en-US
$enPath = "src/KyraFiles.App/Strings/en-US/Resources.resw"
$en = [System.IO.File]::ReadAllText($enPath)
$en = $en.Replace("<value>Files ran into a problem that the developers didn't prepare for yet.</value>", "<value>KyraFiles ran into a problem that the developers didn't prepare for yet.</value>")
$en = $en.Replace("<value>Files</value>", "<value>KyraFiles</value>")
$en = $en.Replace("<value>Update Files</value>", "<value>Update KyraFiles</value>")
$en = $en.Replace("<value>Files cannot access GitHub right now.</value>", "<value>KyraFiles cannot access GitHub right now.</value>")
$en = $en.Replace("<value>Files is running as administrator</value>", "<value>KyraFiles is running as administrator</value>")
$en = $en.Replace("<value>Files can't initialize this directory as a Git repository.</value>", "<value>KyraFiles can't initialize this directory as a Git repository.</value>")
$en = $en.Replace("<value>Files is still running in the background to improve startup performance.</value>", "<value>KyraFiles is still running in the background to improve startup performance.</value>")
$en = $en.Replace("<value>Choose the search engine used for searches in Files</value>", "<value>Choose the search engine used for searches in KyraFiles</value>")
$en = $en.Replace("<value>Files Native (Windows Search / Win32)</value>", "<value>KyraFiles Native (Windows Search / Win32)</value>")
$en = $en.Replace("<value>Files Custom is using {0} across {1} active process(es)</value>", "<value>KyraFiles is using {0} across {1} active process(es)</value>")
$en = $en.Replace("<value>Checks if a newer version of Files Custom has been published.</value>", "<value>Checks if a newer version of KyraFiles has been published.</value>")
$en = $en.Replace("<value>Set Files as the default file manager</value>", "<value>Set KyraFiles as the default file manager</value>")
$en = $en.Replace("<value>Use Files as Open File Dialog</value>", "<value>Use KyraFiles as Open File Dialog</value>")
$en = $en.Replace("<value>Open tab in existing window when opening Files from another app</value>", "<value>Open tab in existing window when opening KyraFiles from another app</value>")
$en = $en.Replace("<value>Open Files on Windows Startup</value>", "<value>Open KyraFiles on Windows Startup</value>")
$en = $en.Replace("<value>Where did Files go?</value>", "<value>Where did KyraFiles go?</value>")
$en = $en.Replace("<value>Show Files icon in the system tray</value>", "<value>Show KyraFiles icon in the system tray</value>")
[System.IO.File]::WriteAllText($enPath, $en, [System.Text.Encoding]::UTF8)

# 2. pt-BR
$ptPath = "src/KyraFiles.App/Strings/pt-BR/Resources.resw"
$pt = [System.IO.File]::ReadAllText($ptPath)
$pt = $pt.Replace("<value>O Files encontrou um problema para o qual os desenvolvedores ainda não se prepararam.</value>", "<value>O KyraFiles encontrou um problema para o qual os desenvolvedores ainda não se prepararam.</value>")
$pt = $pt.Replace("<value>Definir o Files como o gerenciador de arquivos padrão</value>", "<value>Definir o KyraFiles como o gerenciador de arquivos padrão</value>")
$pt = $pt.Replace("<value>Usar o Files como diálogo para Abrir Arquivos</value>", "<value>Usar o KyraFiles como diálogo para Abrir Arquivos</value>")
$pt = $pt.Replace("<value>Abrir aba na janela existente ao abrir o Files a partir de outro programa</value>", "<value>Abrir aba na janela existente ao abrir o KyraFiles a partir de outro programa</value>")
$pt = $pt.Replace("<value>Executar o Files na inicialização do Windows</value>", "<value>Executar o KyraFiles na inicialização do Windows</value>")
$pt = $pt.Replace("equipe do Files por qualquer perda de dados.", "equipe do KyraFiles por qualquer perda de dados.")
$pt = $pt.Replace("<value>Files não pode acessar o GitHub neste momento.</value>", "<value>KyraFiles não pode acessar o GitHub neste momento.</value>")
$pt = $pt.Replace("<value>Files está sendo executado como administrador</value>", "<value>KyraFiles está sendo executado como administrador</value>")
$pt = $pt.Replace("<value>O Files não pode inicializar este diretório como um repositório Git.</value>", "<value>O KyraFiles não pode inicializar este diretório como um repositório Git.</value>")
$pt = $pt.Replace("A opção de abrir Files no Windows Startup", "A opção de abrir KyraFiles no Windows Startup")
$pt = $pt.Replace("<value>Files ainda está em execução em segundo plano para melhorar o desempenho da inicialização.</value>", "<value>KyraFiles ainda está em execução em segundo plano para melhorar o desempenho da inicialização.</value>")
$pt = $pt.Replace("<value>Aonde o Files foi?</value>", "<value>Aonde o KyraFiles foi?</value>")
$pt = $pt.Replace("<value>Mostrar ícone do Files na bandeja do sistema</value>", "<value>Mostrar ícone do KyraFiles na bandeja do sistema</value>")
$pt = $pt.Replace("<value>Files Nativo (Windows Search / Win32)</value>", "<value>KyraFiles Nativo (Windows Search / Win32)</value>")
$pt = $pt.Replace("<value>Verifica se foi publicada uma versão mais recente do Files Custom.</value>", "<value>Verifica se foi publicada uma versão mais recente do KyraFiles.</value>")
$pt = $pt.Replace("<value>O Files Custom está usando {0} em {1} processo(s) ativo(s)</value>", "<value>O KyraFiles está usando {0} em {1} processo(s) ativo(s)</value>")
[System.IO.File]::WriteAllText($ptPath, $pt, [System.Text.Encoding]::UTF8)

# 3. de-DE
$dePath = "src/KyraFiles.App/Strings/de-DE/Resources.resw"
$de = [System.IO.File]::ReadAllText($dePath)
$de = $de.Replace("<value>Files hatte ein Problem, auf das die Entwickler noch nicht vorbereitet sind.</value>", "<value>KyraFiles hatte ein Problem, auf das die Entwickler noch nicht vorbereitet sind.</value>")
$de = $de.Replace("<value>Files als Standard-Dateimanager festlegen</value>", "<value>KyraFiles als Standard-Dateimanager festlegen</value>")
$de = $de.Replace("<value>Files als Standard-Dateidialog verwenden</value>", "<value>KyraFiles als Standard-Dateidialog verwenden</value>")
$de = $de.Replace("wenn Files von einer anderen App geöffnet wird", "wenn KyraFiles von einer anderen App geöffnet wird")
$de = $de.Replace("<value>Files mit Windows starten</value>", "<value>KyraFiles mit Windows starten</value>")
$de = $de.Replace("<value>Files aktualisieren</value>", "<value>KyraFiles aktualisieren</value>")
$de = $de.Replace("<value>Files kann momentan nicht auf GitHub zugreifen.</value>", "<value>KyraFiles kann momentan nicht auf GitHub zugreifen.</value>")
$de = $de.Replace("<value>Files wird mit Administratorrechten ausgeführt</value>", "<value>KyraFiles wird mit Administratorrechten ausgeführt</value>")
$de = $de.Replace("wenn Files als Admin ausgeführt wird", "wenn KyraFiles als Admin ausgeführt wird")
$de = $de.Replace("kannst du Files weiterhin", "kannst du KyraFiles weiterhin")
$de = $de.Replace("<value>Files kann dieses Verzeichnis nicht als Git-Repository initialisieren.</value>", "<value>KyraFiles kann dieses Verzeichnis nicht als Git-Repository initialisieren.</value>")
$de = $de.Replace("Die Option, Files beim Windows-Start zu öffnen", "Die Option, KyraFiles beim Windows-Start zu öffnen")
$de = $de.Replace("<value>Files wird noch im Hintergrund ausgeführt, um die Start-Leistung zu verbessern.</value>", "<value>KyraFiles wird noch im Hintergrund ausgeführt, um die Start-Leistung zu verbessern.</value>")
$de = $de.Replace("<value>Wohin ist Files verschwunden?</value>", "<value>Wohin ist KyraFiles verschwunden?</value>")
[System.IO.File]::WriteAllText($dePath, $de, [System.Text.Encoding]::UTF8)

# 4. es-ES
$esPath = "src/KyraFiles.App/Strings/es-ES/Resources.resw"
$es = [System.IO.File]::ReadAllText($esPath)
$es = $es.Replace("<value>Files se encontró con un problema para el que los desarrolladores aún no están preparados.</value>", "<value>KyraFiles se encontró con un problema para el que los desarrolladores aún no están preparados.</value>")
$es = $es.Replace("<value>Establecer Files como administrador de archivos predeterminado</value>", "<value>Establecer KyraFiles como administrador de archivos predeterminado</value>")
$es = $es.Replace("<value>Usar Files como diálogo de apertura de archivos</value>", "<value>Usar KyraFiles como diálogo de apertura de archivos</value>")
$es = $es.Replace("al abrir Files desde otra aplicación", "al abrir KyraFiles desde otra aplicación")
$es = $es.Replace("<value>Abrir Files al iniciar Windows</value>", "<value>Abrir KyraFiles al iniciar Windows</value>")
$es = $es.Replace("al equipo de Files de ninguna pérdida", "al equipo de KyraFiles de ninguna pérdida")
$es = $es.Replace("<value>Files no tiene acceso a GitHub en este momento.</value>", "<value>KyraFiles no tiene acceso a GitHub en este momento.</value>")
$es = $es.Replace("<value>Files se está ejecutando como administrador</value>", "<value>KyraFiles se está ejecutando como administrador</value>")
$es = $es.Replace("cuando se ejecuta Files como administrador", "cuando se ejecuta KyraFiles como administrador")
$es = $es.Replace("puede seguir utilizando Files sin arrastrar", "puede seguir utilizando KyraFiles sin arrastrar")
$es = $es.Replace("<value>Files no puede inicializar este directorio como repositorio Git.</value>", "<value>KyraFiles no puede inicializar este directorio como repositorio Git.</value>")
$es = $es.Replace("La opción de abrir Files al iniciar Windows", "La opción de abrir KyraFiles al iniciar Windows")
$es = $es.Replace("<value>Files sigue ejecutándose en segundo plano para mejorar el rendimiento al arrancar.</value>", "<value>KyraFiles sigue ejecutándose en segundo plano para mejorar el rendimiento al arrancar.</value>")
$es = $es.Replace("<value>Mostrar icono de Files en la bandeja del sistema</value>", "<value>Mostrar icono de KyraFiles en la bandeja del sistema</value>")
[System.IO.File]::WriteAllText($esPath, $es, [System.Text.Encoding]::UTF8)

Write-Host "Recursos .resw em en-US, pt-BR, de-DE e es-ES atualizados para KyraFiles!"

