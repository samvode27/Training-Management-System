# 🧩 Session 1 – Dependency Injection (DI)


## 📌 Overview
Learned how to use Dependency Injection (DI) in ASP.NET Core.

- Reduce tight coupling
- Improve testability
- Make code maintainable

---

## 🧠 Key Concepts

### 🔹 What is DI?
Instead of creating objects manually, they are provided by the framework.

---

## 🏗️ Implementation

### Interface
```csharp
public interface IEmailService
{
    void Send(string message);
}
