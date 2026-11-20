<p align="center">
  <img alt="KyraFiles" src="./assets/KyraFilesLogo.png" width="240" />
</p>

<p align="center">
  <a style="text-decoration:none" href="https://github.com/alxchs/KyraFiles/actions/workflows/ci.yml">
    <img src="https://github.com/alxchs/KyraFiles/actions/workflows/ci.yml/badge.svg" alt="KyraFiles CI Status" /></a>
</p>

KyraFiles é um gerenciador de arquivos moderno para Windows, pensado para organizar arquivos e pastas com rapidez, abas, painéis e busca integrada.

## Instalação

As versões instaláveis serão publicadas na página de [releases](https://github.com/alxchs/KyraFiles/releases) deste repositório.

## Compilar a partir do código-fonte

O projeto é uma solução .NET com WinUI 3. Na raiz do repositório:

```powershell
msbuild -restore KyraFiles.slnx -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
```

## Contribuir

Abra uma [issue](https://github.com/alxchs/KyraFiles/issues) descrevendo a ideia antes de enviar um [pull request](https://github.com/alxchs/KyraFiles/pulls). Veja também o [guia de contribuição](./CONTRIBUTING.md).

## Licença

Veja [LICENSE-MIT](../LICENSE-MIT), [LICENSE-MPL](../LICENSE-MPL) e [NOTICE.md](./NOTICE.md) para os avisos de licença e de terceiros.
