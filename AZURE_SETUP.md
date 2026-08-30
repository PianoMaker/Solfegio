# Налаштування Azure Pipelines / Azure Pipelines Setup

[English below]

## Українська

### Як підключити репозиторій до Azure DevOps

Цей репозиторій тепер містить файл `azure-pipelines.yml`, який дозволяє Azure Pipelines автоматично збирати проект.

#### Кроки для налаштування:

1. **Відкрийте Azure DevOps**: Перейдіть на https://dev.azure.com

2. **Створіть або виберіть проект**: 
   - Якщо у вас немає проекту, створіть новий
   - Або виберіть існуючий проект

3. **Додайте новий Pipeline**:
   - Перейдіть до розділу "Pipelines" в меню зліва
   - Натисніть "Create Pipeline" або "New Pipeline"

4. **Виберіть GitHub як джерело**:
   - Оберіть "GitHub" у списку джерел коду
   - Авторизуйтесь у GitHub, якщо потрібно

5. **Виберіть репозиторій**:
   - Знайдіть та виберіть репозиторій `PianoMaker/Solfegio`

6. **Підтвердіть конфігурацію**:
   - Azure автоматично знайде файл `azure-pipelines.yml`
   - Перегляньте налаштування та натисніть "Run"

7. **Готово!** Тепер Azure бачить ваш репозиторій і буде автоматично збирати проект при кожному коміті.

### Що робить Pipeline:

- ✅ Встановлює .NET 8 SDK
- ✅ Відновлює NuGet пакети
- ✅ Збирає проект у режимі Release
- ✅ Публікує веб-додаток
- ✅ Створює артефакти для розгортання

---

## English

### How to Connect Repository to Azure DevOps

This repository now contains an `azure-pipelines.yml` file that allows Azure Pipelines to automatically build the project.

#### Setup Steps:

1. **Open Azure DevOps**: Go to https://dev.azure.com

2. **Create or Select a Project**:
   - If you don't have a project, create a new one
   - Or select an existing project

3. **Add a New Pipeline**:
   - Go to "Pipelines" section in the left menu
   - Click "Create Pipeline" or "New Pipeline"

4. **Select GitHub as Source**:
   - Choose "GitHub" from the list of code sources
   - Authorize with GitHub if needed

5. **Select Repository**:
   - Find and select the `PianoMaker/Solfegio` repository

6. **Confirm Configuration**:
   - Azure will automatically detect the `azure-pipelines.yml` file
   - Review the settings and click "Run"

7. **Done!** Azure can now see your repository and will automatically build the project on every commit.

### What the Pipeline Does:

- ✅ Installs .NET 8 SDK
- ✅ Restores NuGet packages
- ✅ Builds the project in Release mode
- ✅ Publishes the web application
- ✅ Creates artifacts for deployment

---

## Технічна інформація / Technical Information

**Проект / Project**: RecogniseChord ASP.NET Core Web Application  
**Версія .NET / .NET Version**: 8.0  
**Build Agent**: Ubuntu Latest  

**Тригери / Triggers**:
- Commits to `main`, `master`, `develop` branches
- Pull requests to these branches

**Артефакти / Artifacts**:
- Published web application (zip)
- Available in Azure Pipelines for deployment
