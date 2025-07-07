#include <SFML/Graphics.hpp>
#include <windows.h>
#include <wininet.h>
#include <gdiplus.h>
#include <thread>
#include <future>
#include <string>
#include <filesystem>

#pragma comment(lib, "gdiplus.lib")
#pragma comment(lib, "wininet.lib")
#pragma comment(lib, "urlmon.lib")

using namespace Gdiplus;
namespace fs = std::filesystem;

ULONG_PTR gdiplusToken;
Image* gif = nullptr;
GUID gifDimension = {};
UINT frameCount = 0;
UINT currentFrame = 0;
UINT* frameDelays = nullptr;

bool isRemoteVersionNewer(const std::string& currentVersion) {
    HINTERNET hInternet = InternetOpenA("Updater", INTERNET_OPEN_TYPE_DIRECT, NULL, NULL, 0);
    if (!hInternet) return false;

    HINTERNET hFile = InternetOpenUrlA(hInternet,
        "https://raw.githubusercontent.com/1xtrade/ACCS/refs/heads/main/newversion",
        NULL, 0, INTERNET_FLAG_RELOAD, 0);
    if (!hFile) {
        InternetCloseHandle(hInternet);
        return false;
    }

    char buffer[128] = {};
    DWORD bytesRead = 0;
    InternetReadFile(hFile, buffer, sizeof(buffer) - 1, &bytesRead);
    InternetCloseHandle(hFile);
    InternetCloseHandle(hInternet);

    std::string remote(buffer);
    remote.erase(remote.find_last_not_of(" \t\r\n") + 1);

    try {
        float remoteVer = std::stof(remote);
        float localVer = std::stof(currentVersion);
        return remoteVer > localVer;
    } catch (...) {
        return false;
    }
}

std::wstring getLastversionURL() {
    HINTERNET hInternet = InternetOpenA("Updater", INTERNET_OPEN_TYPE_DIRECT, NULL, NULL, 0);
    if (!hInternet) return L"";

    HINTERNET hFile = InternetOpenUrlA(hInternet,
        "https://raw.githubusercontent.com/1xtrade/ACCS/refs/heads/main/lastversion",
        NULL, 0, INTERNET_FLAG_RELOAD, 0);
    if (!hFile) {
        InternetCloseHandle(hInternet);
        return L"";
    }

    char buffer[1024] = {};
    DWORD bytesRead = 0;
    InternetReadFile(hFile, buffer, sizeof(buffer) - 1, &bytesRead);
    InternetCloseHandle(hFile);
    InternetCloseHandle(hInternet);

    std::string url(buffer);
    url.erase(url.find_last_not_of(" \t\r\n") + 1);
    int len = MultiByteToWideChar(CP_UTF8, 0, url.c_str(), -1, NULL, 0);
    std::wstring wide(len, 0);
    MultiByteToWideChar(CP_UTF8, 0, url.c_str(), -1, &wide[0], len);
    return wide;
}

std::wstring getTempPathFile(const std::wstring& filename) {
    wchar_t buffer[MAX_PATH];
    GetTempPathW(MAX_PATH, buffer);
    return std::wstring(buffer) + filename;
}

bool downloadFilePreserveName(const std::wstring& url, std::wstring& outPath) {
    size_t slash = url.find_last_of(L"/\\");
    std::wstring filename = (slash != std::wstring::npos) ? url.substr(slash + 1) : L"accs_temp.zip";
    outPath = getTempPathFile(filename);
    return URLDownloadToFileW(NULL, url.c_str(), outPath.c_str(), 0, NULL) == S_OK;
}

bool renderGifFrameToTexture(sf::Texture& texture) {
    UINT w = gif->GetWidth(), h = gif->GetHeight();
    Bitmap bmp(w, h, PixelFormat32bppARGB);
    Graphics g(&bmp);
    g.DrawImage(gif, 0, 0, w, h);

    BitmapData data;
    Rect rect(0, 0, w, h);
    if (bmp.LockBits(&rect, ImageLockModeRead, PixelFormat32bppARGB, &data) != Ok)
        return false;

    sf::Image img;
    img.create(w, h);
    for (UINT y = 0; y < h; ++y)
        for (UINT x = 0; x < w; ++x) {
            BYTE* px = (BYTE*)data.Scan0 + y * data.Stride + x * 4;
            img.setPixel(x, y, sf::Color(px[2], px[1], px[0], px[3]));
        }

    bmp.UnlockBits(&data);
    return texture.loadFromImage(img);
}
int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR lpCmdLine, int) {
    if (std::string(lpCmdLine).find("--from-accs") == std::string::npos)
        return 0;

    sf::RenderWindow window(sf::VideoMode(300, 400), "", sf::Style::None);
    window.setFramerateLimit(60);

    sf::Font font;
    font.loadFromFile("fluffyfont.ttf");

    sf::Shader shader;
    shader.loadFromFile("gradient.frag", sf::Shader::Fragment);
    shader.setUniform("colorTopLeft", sf::Glsl::Vec4(1.0, 0.36, 0.40, 1.0));
    shader.setUniform("colorBottomRight", sf::Glsl::Vec4(1.0, 0.53, 0.27, 1.0));

    sf::Text text;
    text.setFont(font);
    text.setCharacterSize(16);
    text.setFillColor(sf::Color::White);
    std::wstring statusLabel = L"Проверка обновления";

    sf::RenderTexture textRT;
    textRT.create(300, 50);
    sf::Sprite textSprite;
    textSprite.setPosition(0.f, 370.f);

    sf::Clock dotClock, gifClock, launchTimer;
    int dotIndex = 0;
    bool launchWhenReady = false;
    bool statusChanged = true;

    text.setString(statusLabel + L".");
    sf::FloatRect b = text.getLocalBounds();
    text.setOrigin(b.width / 2.f, 0.f);
    text.setPosition(150, 0);
    textRT.clear(sf::Color::Transparent);
    textRT.draw(text);
    textRT.display();
    textSprite.setTexture(textRT.getTexture(), true);
    statusChanged = false;

    GdiplusStartupInput gdiInput;
    GdiplusStartup(&gdiplusToken, &gdiInput, NULL);
    gif = Image::FromFile(L"img/Logo/Update/Update.gif");
    gif->GetFrameDimensionsList(&gifDimension, 1);
    frameCount = gif->GetFrameCount(&gifDimension);
    gif->SelectActiveFrame(&gifDimension, currentFrame = 0);

    UINT size = gif->GetPropertyItemSize(PropertyTagFrameDelay);
    PropertyItem* pItem = (PropertyItem*)malloc(size);
    gif->GetPropertyItem(PropertyTagFrameDelay, size, pItem);
    frameDelays = new UINT[frameCount];
    memcpy(frameDelays, pItem->value, frameCount * sizeof(UINT));
    free(pItem);

    sf::Texture gifTex;
    renderGifFrameToTexture(gifTex);
    sf::Sprite gifSprite(gifTex);
    gifSprite.setScale(100.f / gifTex.getSize().x, 100.f / gifTex.getSize().y);
    gifSprite.setPosition((300 - 100) / 2.f, (400 - 100) / 2.f);

    auto versionFuture = std::async(std::launch::async, [] {
        return isRemoteVersionNewer("1.2");
    });

    bool versionChecked = false;
    auto start = std::chrono::steady_clock::now();

    while (window.isOpen()) {
        sf::Event e;
        while (window.pollEvent(e)) {}

        if (dotClock.getElapsedTime().asSeconds() >= 1.f) {
            std::wstring msg = statusLabel;
            for (int i = 0; i <= dotIndex; ++i) msg += L".";
            text.setString(msg);
            sf::FloatRect tb = text.getLocalBounds();
            text.setOrigin(tb.width / 2.f, 0.f);
            text.setPosition(150, 0);
            statusChanged = true;
            dotIndex = (dotIndex + 1) % 3;
            dotClock.restart();
        }

        if (gifClock.getElapsedTime().asMilliseconds() >= frameDelays[currentFrame] * 10) {
            currentFrame = (currentFrame + 1) % frameCount;
            gif->SelectActiveFrame(&gifDimension, currentFrame);
            renderGifFrameToTexture(gifTex);
            gifSprite.setTexture(gifTex, true);
            gifClock.restart();
        }

        if (statusChanged) {
            textRT.clear(sf::Color::Transparent);
            textRT.draw(text);
            textRT.display();
            textSprite.setTexture(textRT.getTexture(), true);
            statusChanged = false;
        }

        window.clear(sf::Color(36, 36, 36));
        window.draw(gifSprite);
        window.draw(textSprite, &shader);
        window.display();

        if (!versionChecked &&
            std::chrono::steady_clock::now() - start > std::chrono::seconds(2) &&
            versionFuture.wait_for(std::chrono::milliseconds(0)) == std::future_status::ready) {

            versionChecked = true;

            if (versionFuture.get()) {
                std::thread([&]() {
                    int res = MessageBoxW(NULL,
                        L"Доступна новая версия программы.\nСкачать и открыть архив?",
                        L"Обновление", MB_YESNO | MB_ICONQUESTION);
                    if (res == IDYES) {
                        statusLabel = L"Загрузка обновления";
                        statusChanged = true;
                        std::wstring url = getLastversionURL(), outPath;
                        if (!url.empty() && downloadFilePreserveName(url, outPath)) {
                            ShellExecuteW(NULL, NULL, outPath.c_str(), NULL, NULL, SW_SHOWNORMAL);
                        } else {
                            MessageBoxW(NULL, L"Не удалось загрузить обновление.", L"Ошибка", MB_OK | MB_ICONERROR);
                        }
                    }
                    statusLabel = L"Запуск программы";
                    statusChanged = true;
                    launchWhenReady = true;
                    launchTimer.restart();
                }).detach();
            } else {
                statusLabel = L"Запуск программы";
                statusChanged = true;
                launchWhenReady = true;
                launchTimer.restart();
            }
        }

        if (launchWhenReady && launchTimer.getElapsedTime().asSeconds() >= 2.f) {
            window.close();
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(10));
    }

    delete[] frameDelays;
    delete gif;
    GdiplusShutdown(gdiplusToken);
    return 0;
}
