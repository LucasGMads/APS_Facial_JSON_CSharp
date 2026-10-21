# APS — Reconhecimento facial em C# com JSON
## Requisitos

- Windows 10/11
- .NET 8 SDK
- VS Code + extensão C# Dev Kit
- Internet na primeira execução para baixar pacotes NuGet

## Executar

1. Execute:

```powershell
dotnet restore
dotnet run
```

## Uso

1. Execute `dotnet run` na pasta do projeto. As pastas de imagens são criadas automaticamente na primeira execução.
2. Coloque uma foto JPG, JPEG, PNG ou BMP com apenas um rosto em uma das pastas:

   ```text
   imagens\
   ├── Nivel 1\
   ├── Nivel 2\
   └── Nivel 3\
   ```

3. Para cadastrar, escolha `1`, informe nome, senha e nível (1, 2 ou 3). O programa mostra as imagens da pasta correspondente ao nível; digite o número da foto que deseja cadastrar.
4. Para entrar, escolha `2` para usar usuário e senha ou `3` para login facial. No login facial, escolha uma foto da lista das três pastas. Use uma foto da mesma pessoa cadastrada; ela não precisa ser a mesma imagem.
5. Após o login, o programa mostra apenas os registros cujo nível é menor ou igual ao nível do usuário.

Se uma pasta não tiver imagens compatíveis, o programa informa quais pastas pode usar. Para voltar ao menu sem selecionar uma foto, digite `0`.

> **Importante:** o nível é escolhido no cadastro apenas para facilitar a apresentação acadêmica. Isso não é seguro para um sistema real.

## Arquivos

- `Program.cs`: cadastro, login por senha, extração/comparação facial e controle de acesso.
- `usuarios.json`: usuários, hashes das senhas e características numéricas das fotos. Inicialmente `[]`.
- `imagens\Nivel 1`, `imagens\Nivel 2` e `imagens\Nivel 3`: fotos selecionáveis pelo programa. No cadastro, são listadas apenas as fotos da pasta do nível escolhido; no login facial, são listadas as fotos das três pastas. Formatos aceitos: JPG, JPEG, PNG e BMP.
- `dados.json`: propriedades rurais **fictícias** com níveis 1, 2 e 3.
- `haarcascade_frontalface_default.xml`: classificador Haar para detectar o rosto.
- `APSFacial.csproj`: dependências OpenCvSharp.

## Como funciona o algoritmo

1. O OpenCV localiza **exatamente um rosto** na imagem.
2. O rosto é convertido para 96×96 pixels e equalizado.
3. Um descritor **LBP simplificado** transforma a textura facial em 256 números (16 regiões × 16 faixas).
4. O programa compara esse vetor com os rostos cadastrados usando distância qui-quadrado.
5. Aceita a menor distância se ela estiver abaixo do limite definido no código (`0.28`) e suficientemente separada da segunda colocada.

Esse limite é **experimental**, não foi calibrado com uma base de testes e pode aceitar uma pessoa errada ou rejeitar a pessoa correta. Fotos com iluminação, expressão ou ângulo diferentes podem falhar. Para testar, use fotos frontais e bem iluminadas. Não é necessário usar a mesma foto do cadastro.

## Avisos

- **Não é autenticação biométrica segura.** Não possui prova de vida; uma fotografia pode ser usada para tentar entrar.
- O arquivo JSON pode ser editado manualmente. Não armazene dados reais nem use para proteger informações confidenciais.
- As senhas são armazenadas como hash PBKDF2 com sal, não em texto puro.
- Os dados ambientais são inventados para a APS, sem conexão com o Ministério do Meio Ambiente.
