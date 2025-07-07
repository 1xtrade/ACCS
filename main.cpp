#define NOMINMAX
#include <windows.h>
#include <SFML/Graphics.hpp>
#include <iostream>
#include <fstream>
#include <shobjidl.h>
#include "cmake-build-accs/resource.h"
#include <filesystem>

std::wstring GetSteamPathFromRegistry() {
    HKEY hKey;
    const wchar_t* subkey = L"SOFTWARE\\WOW6432Node\\Valve\\Steam";
    wchar_t path[512];
    DWORD pathSize = sizeof(path);
    DWORD type = REG_SZ;

    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, subkey, 0, KEY_READ, &hKey) != ERROR_SUCCESS)
        return L"Не найден";

    if (RegQueryValueExW(hKey, L"InstallPath", nullptr, &type, (LPBYTE)path, &pathSize) != ERROR_SUCCESS) {
        RegCloseKey(hKey);
        return L"Не найден";
    }

    RegCloseKey(hKey);
    return std::wstring(path);
}

std::wstring BrowseForFolder(HWND hwnd) {
    std::wstring result;
    IFileDialog* pfd = nullptr;
    if (SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pfd)))) {
        DWORD options;
        pfd->GetOptions(&options);
        pfd->SetOptions(options | FOS_PICKFOLDERS);
        if (SUCCEEDED(pfd->Show(hwnd))) {
            IShellItem* item;
            if (SUCCEEDED(pfd->GetResult(&item))) {
                PWSTR path;
                if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, &path))) {
                    result = path;
                    CoTaskMemFree(path);
                }
                item->Release();
            }
        }
        pfd->Release();
    }
    return result;
}

class SettingsManager {
public:
    static void Save(bool isDark, bool autoLaunch, const std::wstring& steamPath) {
        std::ofstream out("settings.dat", std::ios::binary);
        if (!out) return;
        uint8_t flags = 0;
        if (isDark)     flags |= (1 << 0);
        if (autoLaunch) flags |= (1 << 1);
        flags ^= 0xAF;
        out.write(reinterpret_cast<char*>(&flags), 1);
        uint32_t len = static_cast<uint32_t>(steamPath.size());
        out.write(reinterpret_cast<char*>(&len), 4);
        for (wchar_t ch : steamPath) {
            wchar_t obf = ch ^ 0xB2;
            out.write(reinterpret_cast<char*>(&obf), 2);
        }
    }

    static void Load(bool &isDark, bool &autoLaunch, std::wstring &steamPath) {
        std::ifstream in("settings.dat", std::ios::binary);
        if (!in) {
            steamPath = GetSteamPathFromRegistry();
            return;
        }
        uint8_t flags;
        in.read(reinterpret_cast<char*>(&flags), 1);
        flags ^= 0xAF;
        isDark = flags & (1 << 0);
        autoLaunch = flags & (1 << 1);
        uint32_t len = 0;
        in.read(reinterpret_cast<char*>(&len), 4);
        steamPath.resize(len);
        for (uint32_t i = 0; i < len; ++i) {
            wchar_t ch;
            in.read(reinterpret_cast<char*>(&ch), 2);
            steamPath[i] = ch ^ 0xB2;
        }
        if (steamPath.empty() || steamPath == L"Не найден") {
            steamPath = GetSteamPathFromRegistry();
        }
    }
};

void SetAutoLaunch(bool enabled) {
    HKEY hKey;
    const wchar_t* regPath = L"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    const wchar_t* appName = L"ACCS";
    if (RegOpenKeyExW(HKEY_CURRENT_USER, regPath, 0, KEY_SET_VALUE, &hKey) == ERROR_SUCCESS) {
        if (enabled) {
            wchar_t exePath[MAX_PATH];
            GetModuleFileNameW(NULL, exePath, MAX_PATH);
            RegSetValueExW(hKey, appName, 0, REG_SZ,
                           (const BYTE*)exePath,
                           (DWORD)((wcslen(exePath) + 1) * sizeof(wchar_t)));
        } else {
            RegDeleteValueW(hKey, appName);
        }
        RegCloseKey(hKey);
    }
}
using namespace std;

int main() {
    if (!std::filesystem::exists("update_done.flag")) {
        STARTUPINFOW si = { sizeof(si) };
        PROCESS_INFORMATION pi;
        CreateProcessW(L"Update.exe", L"Update.exe --from-accs", NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
        WaitForSingleObject(pi.hProcess, INFINITE);

        DWORD exitCode = 0;
        GetExitCodeProcess(pi.hProcess, &exitCode);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);

        if (exitCode != 0) {
            return 1;
        }

        std::ofstream flag("update_done.flag");
        flag << "ok";
        flag.close();
    }

    std::filesystem::remove("update_done.flag");

    CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
    sf::RenderWindow window(sf::VideoMode(800, 500), "ACCS", sf::Style::None);
    window.setFramerateLimit(60);

    sf::Texture bgLight, bgDark;
    sf::Texture closeTex, closeHover, closeNight;
    sf::Texture rollTex, rollHover, rollNight;
    sf::Texture settingsTex, settingsHover, settingsNight;
    sf::Texture homeTex, homeHover, homeNight;
    sf::Texture folderTex, folderHover, folderNight;
    sf::Texture thumbOn, thumbOff;
    sf::Texture plusLight, plusHover, plusNight, plusNightHover;
    sf::Texture soonTex, soonHover;

    bgLight.loadFromFile("img/Background/Background_light.png");
    bgDark.loadFromFile("img/Background/Background_night.png");

    closeTex.loadFromFile("img/Logo/close.png");
    closeHover.loadFromFile("img/Logo/close_gradient.png");
    closeNight.loadFromFile("img/Logo/close_nightgradient.png");

    rollTex.loadFromFile("img/Logo/roll.png");
    rollHover.loadFromFile("img/Logo/roll_gradient.png");
    rollNight.loadFromFile("img/Logo/roll_nightgradient.png");

    settingsTex.loadFromFile("img/Logo/settings.png");
    settingsHover.loadFromFile("img/Logo/settings_gradient.png");
    settingsNight.loadFromFile("img/Logo/settings_nightgradient.png");

    homeTex.loadFromFile("img/Logo/home.png");
    homeHover.loadFromFile("img/Logo/home_gradient.png");
    homeNight.loadFromFile("img/Logo/home_nightgradient.png");
    folderTex.loadFromFile("img/Logo/folder.png");
    folderHover.loadFromFile("img/Logo/folder_gradient.png");
    folderNight.loadFromFile("img/Logo/folder_nightgradient.png");

    thumbOn.loadFromFile("img/Logo/on_gradient.png");
    thumbOff.loadFromFile("img/Logo/off_gradient.png");

    plusLight.loadFromFile("img/Logo/plus_light.png");
    plusHover.loadFromFile("img/Logo/plus_gradient.png");
    plusNight.loadFromFile("img/Logo/plus_night.png");
    plusNightHover.loadFromFile("img/Logo/plus_nightgradient.png");

    soonTex.loadFromFile("img/Logo/soon_save.png");
    soonHover.loadFromFile("img/Logo/soon_gradientsave.png");

    sf::Sprite bg;
    sf::Sprite closeBtn(closeTex), rollBtn(rollTex), settingsBtn(settingsTex), folderBtn(folderTex);
    sf::Sprite plusBtn, soonBtn;

    closeBtn.setScale(0.5f, 0.5f);    closeBtn.setPosition(740, 10);
    rollBtn.setScale(0.5f, 0.5f);     rollBtn.setPosition(680, 10);
    settingsBtn.setScale(0.5f, 0.5f); settingsBtn.setPosition(10, 10);
    folderBtn.setScale(40.f / folderTex.getSize().x, 40.f / folderTex.getSize().y);

    bool isDark = false, autoLaunch = false;
    std::wstring steamPath, selectedCfgPath;
    SettingsManager::Load(isDark, autoLaunch, steamPath);
    SetAutoLaunch(autoLaunch);
    bg.setTexture(isDark ? bgDark : bgLight);
    plusBtn.setTexture(isDark ? plusNight : plusLight);
    soonBtn.setTexture(soonTex);

    sf::Font font;
    font.loadFromFile("fluffyfont.ttf");

    sf::Vector2f switchPos(80.f, 80.f);
    sf::Vector2f autoSwitchPos(switchPos.x, switchPos.y + 60.f);

    sf::Text themeText(L"Светлая / Тёмная тема", font, 18);
    themeText.setFillColor(sf::Color::White);
    themeText.setPosition(switchPos.x + 70, switchPos.y + 4);

    sf::Text autoText(L"Автозагрузка", font, 18);
    autoText.setFillColor(sf::Color::White);
    autoText.setPosition(autoSwitchPos.x + 70, autoSwitchPos.y + 4);

    sf::Text steamText(sf::String(L"Путь до Steam: " + steamPath), font, 18);
    steamText.setFillColor(sf::Color::White);
    steamText.setPosition(autoSwitchPos.x + 50.f, autoSwitchPos.y + 88.f);
    folderBtn.setPosition(autoSwitchPos.x, autoSwitchPos.y + 83.f);

    plusBtn.setScale(200.f / plusLight.getSize().x, 330.f / plusLight.getSize().y);
    plusBtn.setPosition(80.f, (window.getSize().y - 330.f) / 2.f);

    soonBtn.setScale(200.f / soonTex.getSize().x, 330.f / soonTex.getSize().y);
    soonBtn.setPosition(window.getSize().x - 80.f - 200.f, (window.getSize().y - 330.f) / 2.f);

    sf::RectangleShape centerBar(sf::Vector2f(30, 30));
    sf::CircleShape leftCap(15), rightCap(15), thumb(12);
    sf::RectangleShape autoBar(sf::Vector2f(30, 30));
    sf::CircleShape autoLeft(15), autoRight(15), autoThumb(12);

    HWND hwnd = window.getSystemHandle();
    HICON hIcon = (HICON)LoadImageW(
        GetModuleHandleW(NULL),
        MAKEINTRESOURCEW(IDI_ICON1),
        IMAGE_ICON,
        0, 0,
        LR_DEFAULTSIZE
    );
    SendMessage(hwnd, WM_SETICON, ICON_BIG, (LPARAM)hIcon);
    SendMessage(hwnd, WM_SETICON, ICON_SMALL, (LPARAM)hIcon);
    bool dragging = false, settingsMode = false, showSwitch = false;
    sf::Vector2i dragOffset;

    while (window.isOpen()) {
        sf::Event event;
        while (window.pollEvent(event)) {
            sf::Vector2i screenMouse = sf::Mouse::getPosition();
            sf::Vector2i localMouse = sf::Mouse::getPosition(window);

            if (event.type == sf::Event::MouseButtonPressed && event.mouseButton.button == sf::Mouse::Left) {
                if (closeBtn.getGlobalBounds().contains((float)localMouse.x, (float)localMouse.y)) {
                    SettingsManager::Save(isDark, autoLaunch, steamPath);
                    window.close();
                } else if (rollBtn.getGlobalBounds().contains((float)localMouse.x, (float)localMouse.y)) {
                    ShowWindow(hwnd, SW_MINIMIZE);
                } else if (settingsBtn.getGlobalBounds().contains((float)localMouse.x, (float)localMouse.y)) {
                    settingsMode = !settingsMode;
                    showSwitch = settingsMode;
                } else if (!showSwitch && plusBtn.getGlobalBounds().contains((float)localMouse.x, (float)localMouse.y)) {
                    IFileDialog* pfd = nullptr;
                    if (SUCCEEDED(CoCreateInstance(CLSID_FileOpenDialog, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pfd)))) {
                        COMDLG_FILTERSPEC filters[] = { {L"CFG файлы", L"*.cfg"} };
                        pfd->SetFileTypes(1, filters);
                        if (SUCCEEDED(pfd->Show(hwnd))) {
                            IShellItem* item;
                            if (SUCCEEDED(pfd->GetResult(&item))) {
                                PWSTR filePath;
                                if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, &filePath))) {
                                    selectedCfgPath = filePath;
                                    CoTaskMemFree(filePath);
                                }
                                item->Release();
                            }
                        }
                        pfd->Release();
                    }

                    if (!selectedCfgPath.empty()) {
                        int res = MessageBoxW(hwnd,
                            L"После нажатия на кнопку \"Да\" — файл будет загружен в папку с конфигами CS 2.\nПродолжить?",
                            L"Выбор действия", MB_YESNO | MB_ICONQUESTION);
                        if (res == IDYES) {
                            std::wstring steamCfg = steamPath + L"\\steamapps\\common\\Counter-Strike Global Offensive\\game\\csgo\\cfg\\";
                            CreateDirectoryW(steamCfg.c_str(), NULL);

                            size_t dot = selectedCfgPath.find_last_of(L'.');
                            std::wstring ext = (dot != std::wstring::npos) ? selectedCfgPath.substr(dot) : L".cfg";
                            std::wstring dst = steamCfg + L"autoexec" + ext;

                            if (CopyFileW(selectedCfgPath.c_str(), dst.c_str(), FALSE)) {
                                MessageBoxW(hwnd, L"Загрузка конфигурации прошла успешно!", L"Успешно!", MB_OK | MB_ICONINFORMATION);
                            }
                        }
                        selectedCfgPath.clear();
                    }
                } else if (showSwitch && sf::FloatRect(switchPos, {60.f, 30.f}).contains((float)localMouse.x, (float)localMouse.y)) {
                    isDark = !isDark;
                    bg.setTexture(isDark ? bgDark : bgLight);
                    plusBtn.setTexture(isDark ? plusNight : plusLight);
                } else if (showSwitch && sf::FloatRect(autoSwitchPos, {60.f, 30.f}).contains((float)localMouse.x, (float)localMouse.y)) {
                    autoLaunch = !autoLaunch;
                    SetAutoLaunch(autoLaunch);
                } else if (showSwitch && folderBtn.getGlobalBounds().contains((float)localMouse.x, (float)localMouse.y)) {
                    std::wstring selected = BrowseForFolder(hwnd);
                    if (!selected.empty()) {
                        steamPath = selected;
                        steamText.setString(L"Путь до Steam: " + steamPath);
                        SettingsManager::Save(isDark, autoLaunch, steamPath);
                    }
                } else {
                    dragging = true;
                    dragOffset = localMouse;
                }
            } else if (event.type == sf::Event::MouseButtonReleased) {
                dragging = false;
            } else if (event.type == sf::Event::MouseMoved && dragging) {
                sf::Vector2i mouseScreen = sf::Mouse::getPosition();
                SetWindowPos(hwnd, nullptr,
                             mouseScreen.x - dragOffset.x,
                             mouseScreen.y - dragOffset.y,
                             0, 0, SWP_NOSIZE | SWP_NOZORDER);
            } else if (event.type == sf::Event::Closed) {
                SettingsManager::Save(isDark, autoLaunch, steamPath);
                window.close();
            }
        }

        sf::Vector2i mouse = sf::Mouse::getPosition(window);
        closeBtn.setTexture(closeBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y)
            ? (isDark ? closeNight : closeHover) : closeTex);
        rollBtn.setTexture(rollBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y)
            ? (isDark ? rollNight : rollHover) : rollTex);
        settingsBtn.setTexture(settingsMode
            ? (settingsBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y)
                ? (isDark ? homeNight : homeHover) : homeTex)
            : (settingsBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y)
                ? (isDark ? settingsNight : settingsHover) : settingsTex));
        folderBtn.setTexture(folderBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y)
            ? (isDark ? folderNight : folderHover) : folderTex);

        if (!showSwitch) {
            bool hoveredPlus = plusBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y);
            plusBtn.setTexture(isDark
                ? (hoveredPlus ? plusNightHover : plusNight)
                : (hoveredPlus ? plusHover : plusLight));

            bool hoveredSoon = soonBtn.getGlobalBounds().contains((float)mouse.x, (float)mouse.y);
            soonBtn.setTexture(hoveredSoon ? soonHover : soonTex);
        }

        centerBar.setPosition(switchPos.x + 15, switchPos.y);
        leftCap.setPosition(switchPos.x, switchPos.y);
        rightCap.setPosition(switchPos.x + 30, switchPos.y);
        thumb.setPosition(switchPos.x + (isDark ? 33 : 3), switchPos.y + 3);
        thumb.setTexture(isDark ? &thumbOff : &thumbOn);

        autoBar.setPosition(autoSwitchPos.x + 15, autoSwitchPos.y);
        autoLeft.setPosition(autoSwitchPos.x, autoSwitchPos.y);
        autoRight.setPosition(autoSwitchPos.x + 30, autoSwitchPos.y);
        autoThumb.setPosition(autoSwitchPos.x + (autoLaunch ? 33 : 3), autoSwitchPos.y + 3);
        autoThumb.setFillColor(autoLaunch ? sf::Color(50, 205, 50) : sf::Color::White);
        if (!autoLaunch) autoThumb.setTexture(&thumbOn);
        else autoThumb.setTexture(nullptr);

        window.clear();
        window.draw(bg);
        window.draw(settingsBtn);
        window.draw(rollBtn);
        window.draw(closeBtn);

        if (showSwitch) {
            window.draw(leftCap); window.draw(centerBar); window.draw(rightCap); window.draw(thumb); window.draw(themeText);
            window.draw(autoLeft); window.draw(autoBar); window.draw(autoRight); window.draw(autoThumb); window.draw(autoText);
            window.draw(folderBtn); window.draw(steamText);
        } else {
            window.draw(plusBtn);
            window.draw(soonBtn);
        }

        window.display();
    }

    CoUninitialize();
    return 0;
}
