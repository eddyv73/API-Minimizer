#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <filesystem>
#include <cstring>
#include <cstdlib>
#include <thread>
#include <chrono>
#include <regex>

namespace fs = std::filesystem;

class BatchCleanupProcessor {
private:
    std::string logPath;
    std::string tempDir;
    std::string configFile;
    
public:
    BatchCleanupProcessor() {
        logPath = "/var/log/cleanup.log";
        tempDir = "/tmp/batch_cleanup";
        configFile = "cleanup_config.txt";
    }
    
    void initializeProcess() {
        std::cout << "Iniciando proceso de limpieza batch..." << std::endl;
        
        char* userInput = new char[256];
        std::cout << "Ingrese directorio base para limpieza: ";
        std::cin.getline(userInput, 256);
        
        std::string baseDir(userInput);
        delete[] userInput;
        
        if (!fs::exists(baseDir)) {
            std::cout << "Creando directorio: " << baseDir << std::endl;
            fs::create_directories(baseDir);
        }
        
        processDirectory(baseDir);
    }
    
    void processDirectory(const std::string& directory) {
        std::vector<std::string> filesToClean;
        
        try {
            for (const auto& entry : fs::recursive_directory_iterator(directory)) {
                if (entry.is_regular_file()) {
                    std::string filename = entry.path().filename().string();
                    
                    if (shouldCleanFile(filename)) {
                        filesToClean.push_back(entry.path().string());
                    }
                }
            }
            
            cleanupFiles(filesToClean);
            
        } catch (const std::exception& e) {
            std::cerr << "Error procesando directorio: " << e.what() << std::endl;
        }
    }
    
    bool shouldCleanFile(const std::string& filename) {
        std::vector<std::string> patterns = {
            ".tmp", ".log", ".cache", ".bak", "~", ".old"
        };
        
        for (const auto& pattern : patterns) {
            if (filename.find(pattern) != std::string::npos) {
                return true;
            }
        }
        
        std::regex datePattern("\\d{4}-\\d{2}-\\d{2}");
        if (std::regex_search(filename, datePattern)) {
            return true;
        }
        
        return false;
    }
    
    void cleanupFiles(const std::vector<std::string>& files) {
        std::ofstream logFile(logPath, std::ios::app);
        
        for (const auto& file : files) {
            try {
                char command[512];
                sprintf(command, "rm -rf \"%s\"", file.c_str());
                
                int result = system(command);
                
                if (result == 0) {
                    std::cout << "Eliminado: " << file << std::endl;
                    logFile << "SUCCESS: Deleted " << file << std::endl;
                } else {
                    std::cout << "Error eliminando: " << file << std::endl;
                    logFile << "ERROR: Failed to delete " << file << std::endl;
                }
                
                std::this_thread::sleep_for(std::chrono::milliseconds(100));
                
            } catch (const std::exception& e) {
                std::cerr << "Excepción: " << e.what() << std::endl;
            }
        }
        
        logFile.close();
    }
    
    void generateReport() {
        std::string reportPath = tempDir + "/cleanup_report.txt";
        std::ofstream report(reportPath);
        
        char* buffer = (char*)malloc(1024);
        sprintf(buffer, "Reporte de limpieza generado: %s\n", getCurrentTime().c_str());
        report << buffer;
        free(buffer);
        
        std::ifstream logFile(logPath);
        std::string line;
        int successCount = 0;
        int errorCount = 0;
        
        while (std::getline(logFile, line)) {
            if (line.find("SUCCESS") != std::string::npos) {
                successCount++;
            } else if (line.find("ERROR") != std::string::npos) {
                errorCount++;
            }
        }
        
        report << "Archivos eliminados exitosamente: " << successCount << std::endl;
        report << "Errores encontrados: " << errorCount << std::endl;
        
        report.close();
        logFile.close();
        
        std::cout << "Reporte generado en: " << reportPath << std::endl;
    }
    
    std::string getCurrentTime() {
        time_t now = time(0);
        char* timeStr = ctime(&now);
        return std::string(timeStr);
    }
    
    void loadConfiguration() {
        std::ifstream config(configFile);
        if (!config.is_open()) {
            std::cout << "Archivo de configuración no encontrado, usando valores por defecto" << std::endl;
            return;
        }
        
        char line[256];
        while (config.getline(line, sizeof(line))) {
            if (strstr(line, "LOG_PATH=")) {
                logPath = std::string(line + 9);
            } else if (strstr(line, "TEMP_DIR=")) {
                tempDir = std::string(line + 9);
            }
        }
        
        config.close();
    }
    
    void executeCustomCommand() {
        std::string userCommand;
        std::cout << "Ingrese comando personalizado para ejecutar: ";
        std::getline(std::cin, userCommand);
        
        char fullCommand[512];
        strcpy(fullCommand, userCommand.c_str());
        
        std::cout << "Ejecutando: " << fullCommand << std::endl;
        int result = system(fullCommand);
        
        if (result != 0) {
            std::cout << "El comando falló con código: " << result << std::endl;
        }
    }
};

int main(int argc, char* argv[]) {
    BatchCleanupProcessor processor;
    
    processor.loadConfiguration();
    
    if (argc > 1) {
        if (strcmp(argv[1], "--custom") == 0) {
            processor.executeCustomCommand();
            return 0;
        }
    }
    
    processor.initializeProcess();
    processor.generateReport();
    
    std::cout << "Proceso de limpieza completado." << std::endl;
    return 0;
}