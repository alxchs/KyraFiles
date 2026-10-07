// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Data.Models;
using Files.App.Utils.Shell;
using Microsoft.UI.Xaml;

namespace Files.App.Services
{
	/// <summary>
	/// Provides metadata and formatted values for Windows Explorer Property System columns.
	/// </summary>
	public sealed class ExplorerPropertyService : IExplorerPropertyService
	{
		private static readonly List<DynamicColumnDefinition> Catalog =
		[
			// Image
			new() { CanonicalName = "System.Image.Dimensions", DisplayName = "Dimensões", Category = "Imagem", UserLength = new GridLength(140) },
			new() { CanonicalName = "System.Photo.DateTaken", DisplayName = "Data de captura", Category = "Imagem", UserLength = new GridLength(150) },
			new() { CanonicalName = "System.Photo.CameraModel", DisplayName = "Modelo da câmera", Category = "Imagem", UserLength = new GridLength(150) },
			new() { CanonicalName = "System.Photo.CameraManufacturer", DisplayName = "Fabricante da câmera", Category = "Imagem", UserLength = new GridLength(150) },
			new() { CanonicalName = "System.Photo.FNumber", DisplayName = "Abertura F", Category = "Imagem", UserLength = new GridLength(100), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Photo.ISOSpeed", DisplayName = "Velocidade ISO", Category = "Imagem", UserLength = new GridLength(100), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Image.HorizontalSize", DisplayName = "Largura", Category = "Imagem", UserLength = new GridLength(100), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Image.VerticalSize", DisplayName = "Altura", Category = "Imagem", UserLength = new GridLength(100), TextAlignment = TextAlignment.Right },

			// Media / Audio
			new() { CanonicalName = "System.Media.Duration", DisplayName = "Duração", Category = "Mídia", UserLength = new GridLength(100), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Music.Artist", DisplayName = "Artistas participantes", Category = "Mídia", UserLength = new GridLength(180) },
			new() { CanonicalName = "System.Music.AlbumTitle", DisplayName = "Álbum", Category = "Mídia", UserLength = new GridLength(180) },
			new() { CanonicalName = "System.Music.AlbumArtist", DisplayName = "Artista do álbum", Category = "Mídia", UserLength = new GridLength(180) },
			new() { CanonicalName = "System.Music.TrackNumber", DisplayName = "Número da faixa", Category = "Mídia", UserLength = new GridLength(80), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Music.Genre", DisplayName = "Gênero", Category = "Mídia", UserLength = new GridLength(120) },
			new() { CanonicalName = "System.Media.Year", DisplayName = "Ano", Category = "Mídia", UserLength = new GridLength(80), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Audio.EncodingBitrate", DisplayName = "Taxa de bits", Category = "Mídia", UserLength = new GridLength(100), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Audio.SampleRate", DisplayName = "Taxa de amostragem", Category = "Mídia", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },

			// Video
			new() { CanonicalName = "System.Video.FrameWidth", DisplayName = "Largura do quadro", Category = "Vídeo", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Video.FrameHeight", DisplayName = "Altura do quadro", Category = "Vídeo", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Video.FrameRate", DisplayName = "Taxa de quadros", Category = "Vídeo", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Video.TotalBitrate", DisplayName = "Taxa de bits total", Category = "Vídeo", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Media.Director", DisplayName = "Diretores", Category = "Vídeo", UserLength = new GridLength(160) },
			new() { CanonicalName = "System.Media.Producer", DisplayName = "Produtores", Category = "Vídeo", UserLength = new GridLength(160) },

			// Document
			new() { CanonicalName = "System.Author", DisplayName = "Autores", Category = "Documento", UserLength = new GridLength(160) },
			new() { CanonicalName = "System.Title", DisplayName = "Título", Category = "Documento", UserLength = new GridLength(180) },
			new() { CanonicalName = "System.Subject", DisplayName = "Assunto", Category = "Documento", UserLength = new GridLength(160) },
			new() { CanonicalName = "System.Keywords", DisplayName = "Palavras-chave", Category = "Documento", UserLength = new GridLength(140) },
			new() { CanonicalName = "System.Comment", DisplayName = "Comentários", Category = "Documento", UserLength = new GridLength(180) },
			new() { CanonicalName = "System.Document.PageCount", DisplayName = "Páginas", Category = "Documento", UserLength = new GridLength(80), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Document.WordCount", DisplayName = "Contagem de palavras", Category = "Documento", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },
			new() { CanonicalName = "System.Document.CharacterCount", DisplayName = "Contagem de caracteres", Category = "Documento", UserLength = new GridLength(120), TextAlignment = TextAlignment.Right },

			// File / Description
			new() { CanonicalName = "System.FileVersion", DisplayName = "Versão do arquivo", Category = "Arquivo", UserLength = new GridLength(120) },
			new() { CanonicalName = "System.Rating", DisplayName = "Classificação", Category = "Arquivo", UserLength = new GridLength(100) },
			new() { CanonicalName = "System.Copyright", DisplayName = "Direitos autorais", Category = "Arquivo", UserLength = new GridLength(180) },
			new() { CanonicalName = "System.ApplicationName", DisplayName = "Nome do aplicativo", Category = "Arquivo", UserLength = new GridLength(160) },
			new() { CanonicalName = "System.Company", DisplayName = "Empresa", Category = "Arquivo", UserLength = new GridLength(160) },
			new() { CanonicalName = "System.ComputerName", DisplayName = "Computador", Category = "Arquivo", UserLength = new GridLength(120) },
			new() { CanonicalName = "System.FileOwner", DisplayName = "Proprietário", Category = "Arquivo", UserLength = new GridLength(160) },
			new() { CanonicalName = "System.FileAttributes", DisplayName = "Atributos", Category = "Arquivo", UserLength = new GridLength(100) },
		];

		/// <inheritdoc/>
		public IReadOnlyList<DynamicColumnDefinition> GetAllAvailableProperties()
		{
			return Catalog;
		}

		/// <inheritdoc/>
		public string? GetFormattedProperty(string filePath, string canonicalName)
		{
			if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(canonicalName))
				return null;

			try
			{
				using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(filePath);
				return shellItem?.Properties?.GetPropertyString(canonicalName);
			}
			catch
			{
				return null;
			}
		}
	}
}
