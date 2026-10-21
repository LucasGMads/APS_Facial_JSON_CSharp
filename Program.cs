using System.Security.Cryptography;
using System.Text.Json;
using OpenCvSharp;

const string ArquivoUsuarios = "usuarios.json";
const string ArquivoDados = "dados.json";
const string PastaImagens = "imagens";
var json = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

List<Usuario> usuarios = Ler<List<Usuario>>(ArquivoUsuarios) ?? new();
List<Registro> dados = Ler<List<Registro>>(ArquivoDados) ?? new();
CriarPastasImagens();

while (true)
{
    Console.WriteLine("\n=== APS - LOGIN FACIAL / SENHA ===");
    Console.WriteLine("1 - Cadastrar usuário e foto");
    Console.WriteLine("2 - Entrar com senha");
    Console.WriteLine("3 - Entrar com foto");
    Console.WriteLine("0 - Sair");
    Console.Write("Opção: ");
    switch (Console.ReadLine())
    {
        case "1": Cadastrar(); break;
        case "2": EntrarSenha(); break;
        case "3": EntrarFoto(); break;
        case "0": return;
        default: Console.WriteLine("Opção inválida."); break;
    }
}

T? Ler<T>(string arquivo)
{
    if (!File.Exists(arquivo)) return default;
    return JsonSerializer.Deserialize<T>(File.ReadAllText(arquivo), json);
}
void Salvar() => File.WriteAllText(ArquivoUsuarios, JsonSerializer.Serialize(usuarios, json));

void Cadastrar()
{
    Console.Write("Nome de usuário: ");
    string nome = (Console.ReadLine() ?? "").Trim();
    if (nome.Length == 0 || usuarios.Any(u => u.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)))
    { Console.WriteLine("Nome vazio ou já cadastrado."); return; }

    Console.Write("Senha: ");
    string senha = Console.ReadLine() ?? "";
    if (senha.Length < 4) { Console.WriteLine("Use pelo menos 4 caracteres (apenas demonstração)."); return; }

    // Os níveis são definidos pelo operador do protótipo, não por um serviço real.
    Console.Write("Nível de acesso (1, 2 ou 3): ");
    if (!int.TryParse(Console.ReadLine(), out int nivel) || nivel < 1 || nivel > 3)
    { Console.WriteLine("Nível inválido."); return; }

    string? caminhoFoto = SelecionarImagem(nivel);
    if (caminhoFoto == null) return;
    var face = ExtrairFace(caminhoFoto);
    if (face == null) return;

    byte[] sal = RandomNumberGenerator.GetBytes(16);
    string hash = Convert.ToBase64String(
        Rfc2898DeriveBytes.Pbkdf2(senha, sal, 120_000, HashAlgorithmName.SHA256, 32));
    usuarios.Add(new Usuario(nome, nivel, Convert.ToBase64String(sal), hash, face));
    Salvar();
    Console.WriteLine("Usuário e rosto cadastrados em usuarios.json.");
}
void EntrarSenha()
{
    Console.Write("Usuário: ");
    string nome = Console.ReadLine() ?? "";
    Console.Write("Senha: ");
    string senha = Console.ReadLine() ?? "";
    var u = usuarios.FirstOrDefault(x => x.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase));
    if (u == null) { Console.WriteLine("Acesso negado."); return; }
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        senha, Convert.FromBase64String(u.Sal), 120_000, HashAlgorithmName.SHA256, 32);
    if (!CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(u.HashSenha)))
    { Console.WriteLine("Acesso negado."); return; }
    MostrarDados(u);
}
void EntrarFoto()
{
    string? caminhoFoto = SelecionarImagem();
    if (caminhoFoto == null) return;
    var face = ExtrairFace(caminhoFoto);
    if (face == null) return;
    var ranking = usuarios.Select(u => (usuario: u, distancia: Distancia(face, u.Face)))
                          .OrderBy(x => x.distancia).ToList();
    if (ranking.Count == 0 || ranking[0].distancia > 0.28 ||
        (ranking.Count > 1 && ranking[1].distancia - ranking[0].distancia < 0.04))
    { Console.WriteLine("Rosto não reconhecido. Tente outra foto ou entre por senha."); return; }
    Console.WriteLine($"Correspondência experimental: {ranking[0].distancia:F3}");
    MostrarDados(ranking[0].usuario);
}

string CaminhoPastaNivel(int nivel) =>
    Path.Combine(Directory.GetCurrentDirectory(), PastaImagens, $"Nivel {nivel}");

void CriarPastasImagens()
{
    for (int nivel = 1; nivel <= 3; nivel++)
        Directory.CreateDirectory(CaminhoPastaNivel(nivel));
}

string? SelecionarImagem(int? nivel = null)
{
    var imagens = Enumerable.Range(1, 3)
        .Where(n => nivel == null || n == nivel)
        .SelectMany(n => Directory.GetFiles(CaminhoPastaNivel(n))
            .Where(ArquivoDeImagemSuportado)
            .Select(caminho => (Caminho: caminho, Nivel: n)))
        .OrderBy(imagem => imagem.Nivel)
        .ThenBy(imagem => Path.GetFileName(imagem.Caminho), StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (imagens.Count == 0)
    {
        string local = nivel.HasValue
            ? $"imagens\\Nivel {nivel.Value}"
            : "imagens\\Nivel 1, imagens\\Nivel 2 ou imagens\\Nivel 3";
        Console.WriteLine($"Nenhuma imagem encontrada. Coloque arquivos JPG, JPEG, PNG ou BMP em: {local}");
        return null;
    }

    Console.WriteLine("\nImagens disponíveis:");
    for (int i = 0; i < imagens.Count; i++)
    {
        string caminhoRelativo = Path.GetRelativePath(
            Path.Combine(Directory.GetCurrentDirectory(), PastaImagens), imagens[i].Caminho);
        Console.WriteLine($"{i + 1} - {caminhoRelativo}");
    }
    Console.WriteLine("0 - Voltar");
    Console.Write("Escolha o número da foto: ");

    if (!int.TryParse(Console.ReadLine(), out int escolha) ||
        escolha < 0 || escolha > imagens.Count)
    {
        Console.WriteLine("Opção inválida.");
        return null;
    }
    if (escolha == 0) return null;

    return imagens[escolha - 1].Caminho;
}

bool ArquivoDeImagemSuportado(string caminho)
{
    string extensao = Path.GetExtension(caminho);
    return extensao.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || extensao.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || extensao.Equals(".png", StringComparison.OrdinalIgnoreCase)
        || extensao.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
}

void MostrarDados(Usuario u)
{
    Console.WriteLine($"\nBem-vindo(a), {u.Nome}! Nível {u.Nivel}");
    foreach (var d in dados.Where(d => d.Nivel <= u.Nivel))
        Console.WriteLine($"[{d.Nivel}] {d.Propriedade} - {d.Municipio}: {d.Descricao}");
}

// Demonstração: detector Haar + descritor LBP simples, sem rede neural.
// NÃO é uma solução segura de autenticação; fotos impressas podem enganar o sistema.
float[]? ExtrairFace(string caminho)
{
    if (!File.Exists(caminho)) { Console.WriteLine("Arquivo não encontrado."); return null; }
    try
    {
        using var imagem = Cv2.ImRead(caminho, ImreadModes.Grayscale);
        if (imagem.Empty()) throw new Exception("Imagem inválida.");
        using var detector = new CascadeClassifier(
            Path.Combine(AppContext.BaseDirectory, "haarcascade_frontalface_default.xml"));
        var rostos = detector.DetectMultiScale(imagem, 1.1, 5, minSize: new Size(60, 60));
        if (rostos.Length != 1)
        { Console.WriteLine("Use uma imagem com exatamente um rosto visível."); return null; }
        using var recorte = new Mat(imagem, rostos[0]);
        using var padrao = new Mat();
        Cv2.Resize(recorte, padrao, new Size(96, 96));
        Cv2.EqualizeHist(padrao, padrao);
        // Histograma de padrões binários locais (LBP), dividido em 4x4 regiões.
        float[] hist = new float[4 * 4 * 16];
        for (int y = 1; y < 95; y++)
        for (int x = 1; x < 95; x++)
        {
            byte centro = padrao.At<byte>(y, x);
            int[] dy = {-1,-1,-1,0,1,1,1,0};
            int[] dx = {-1,0,1,1,1,0,-1,-1};
            int codigo = 0;
            for (int k = 0; k < 8; k++)
                if (padrao.At<byte>(y + dy[k], x + dx[k]) >= centro) codigo |= 1 << k;
            int regiao = Math.Min(y / 24, 3) * 4 + Math.Min(x / 24, 3);
            hist[regiao * 16 + (codigo >> 4)]++;
        }
        for (int regiao = 0; regiao < 16; regiao++)
        {
            float total = 0;
            for (int b = 0; b < 16; b++) total += hist[regiao * 16 + b];
            for (int b = 0; b < 16; b++) hist[regiao * 16 + b] /= total;
        }
        return hist;
    }
    catch (Exception ex) { Console.WriteLine("Erro ao ler foto: " + ex.Message); return null; }
}
double Distancia(float[] a, float[] b)
{
    if (a.Length != b.Length) return double.MaxValue;
    double soma = 0;
    for (int i = 0; i < a.Length; i++)
    {
        double dif = a[i] - b[i];
        soma += dif * dif / (a[i] + b[i] + 0.000001);
    }
    return soma / 32.0; // distância chi-quadrado média (0 é igual)
}

record Usuario(string Nome, int Nivel, string Sal, string HashSenha, float[] Face);
record Registro(int Nivel, string Propriedade, string Municipio, string Descricao);
