using System;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Xabe.FFmpeg;

// Интерфейс команды
public interface ICommand
{
    Task ExecuteAsync();
}

// Команда для получения информации о видео
public class GetVideoInfoCommand : ICommand
{
    private readonly string _videoUrl;
    private readonly YoutubeClient _youtubeClient;

    public GetVideoInfoCommand(string videoUrl)
    {
        _videoUrl = videoUrl;
        _youtubeClient = new YoutubeClient();
    }

    public async Task ExecuteAsync()
    {
        try
        {
            Video video = await _youtubeClient.Videos.GetAsync(_videoUrl);
            Console.WriteLine($"Название видео: {video.Title}");
            Console.WriteLine($"Описание: {video.Description}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении информации о видео: {ex.Message}");
        }
    }
}

// Команда для скачивания и кодирования видео
public class DownloadVideoCommand : ICommand
{
    private readonly string _videoUrl;
    private readonly string _outputFilePath;
    private readonly YoutubeClient _youtubeClient;

    public DownloadVideoCommand(string videoUrl, string outputFilePath)
    {
        _videoUrl = videoUrl;
        _outputFilePath = outputFilePath;
        _youtubeClient = new YoutubeClient();
    }

    public async Task ExecuteAsync()
    {
        try
        {
            var video = await _youtubeClient.Videos.GetAsync(_videoUrl);
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

            if (streamManifest != null)
            {
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                await _youtubeClient.Videos.Streams.DownloadAsync(streamInfo, _outputFilePath);

                await EncodeVideoAsync(_outputFilePath);
            }
            else
            {
                Console.WriteLine("Ошибка: Видео недоступно.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при скачивании и кодировании видео: {ex.Message}");
        }
    }

    private async Task EncodeVideoAsync(string inputFilePath)
    {
        try
        {
            string outputFilePath = Path.ChangeExtension(inputFilePath, "mp4");

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputFilePath);
            IConversion conversion = FFmpeg.Conversions.New();

            conversion.OnProgress += (sender, args) => Console.WriteLine($"Процент завершения: {args.Percent}%");

            await conversion.AddStream(mediaInfo.VideoStreams.First())
                             .SetOutput(outputFilePath)
                             .Start();

            File.Delete(inputFilePath); // Удаление исходного файла после кодирования
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при кодировании видео: {ex.Message}");
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Введите ссылку на Youtube-видео: ");
        string videoUrl = Console.ReadLine();

        Console.Write("Укажите путь для сохранения видео: ");
        string outputFilePath = Console.ReadLine();

        ICommand getInfoCommand = new GetVideoInfoCommand(videoUrl);
        ICommand downloadCommand = new DownloadVideoCommand(videoUrl, outputFilePath);

        try
        {
            await getInfoCommand.ExecuteAsync();
            await downloadCommand.ExecuteAsync();

            Console.WriteLine("Видео успешно скачано, закодировано и информация получена.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Общая ошибка: {ex.Message}");
        }
    }
}
