namespace RbacApp.Application.Common.Interfaces;

/// <summary>
/// provisioning دیتابیس فیزیکی برای یک tenant جدید:
/// ایجاد دیتابیس، اعمال migration و seed نقش‌های پیش‌فرض.
/// </summary>
public interface ITenantDbProvisioner
{
    /// <summary>
    /// دیتابیس فیزیکی را برای connection داده‌شده ایجاد و migrate می‌کند.
    /// </summary>
    Task ProvisionAsync(string connectionString, CancellationToken ct = default);
}
