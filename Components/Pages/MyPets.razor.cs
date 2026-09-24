using Microsoft.AspNetCore.Components;
using Shoppet_VetClinic.Models;
using Shoppet_VetClinic.Services;

namespace Shoppet_VetClinic.Components.Pages;

public partial class MyPets
{
    // =========================================================
    // INJECTED SERVICES
    // =========================================================

    [Inject] private DatabaseService Db { get; set; } = default!;
    [Inject] private AuthService Auth { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // =========================================================
    // STATE
    // =========================================================

    public List<PetProfile> Pets { get; private set; } = new();

    public bool IsPremium { get; private set; }
    public bool CanAddMorePets { get; private set; }

    public bool ShowAddPetForm { get; private set; }
    public bool IsSaving { get; private set; }

    public string StatusMessage { get; private set; } = string.Empty;
    public bool IsError { get; private set; }

    // New pet form fields
    public string NewPetName { get; set; } = string.Empty;
    public string NewPetSpecies { get; set; } = "Dog";
    public string NewPetBreed { get; set; } = string.Empty;
    public string NewPetAge { get; set; } = string.Empty;
    public decimal? NewPetWeightKg { get; set; }
    public string NewPetDiet { get; set; } = string.Empty;

    private bool _initialized;

    // =========================================================
    // LIFECYCLE
    // =========================================================

    protected override async Task OnParametersSetAsync()
    {
        await Auth.InitializeAsync();

        if (_initialized)
            return;

        _initialized = true;
        Load();
    }

    // =========================================================
    // LOAD
    // =========================================================

    private void Load()
    {
        if (!Auth.IsLoggedIn)
        {
            Nav.NavigateTo("/login");
            return;
        }

        // Only pet owners manage their own pets
        if (!Auth.IsPetOwner && !Auth.IsAdmin)
        {
            Nav.NavigateTo("/dashboard");
            return;
        }

        IsPremium = Auth.IsPremium;
        CanAddMorePets = IsPremium || Pets.Count < 1;

        Pets = Db.GetPetsByUser(Auth.CurrentUser!.Id) ?? new();

        // Re-evaluate the limit now that we have the pet count
        CanAddMorePets = IsPremium || Pets.Count < 1;
    }

    // =========================================================
    // FORM
    // =========================================================

    public void ShowAddForm()
    {
        if (!CanAddMorePets)
        {
            IsError = true;
            StatusMessage =
                "Free accounts support 1 pet. Upgrade to Premium to add more.";
            return;
        }

        StatusMessage = string.Empty;
        IsError = false;
        ShowAddPetForm = true;
    }

    public void HideAddForm()
    {
        ShowAddPetForm = false;

        NewPetName = string.Empty;
        NewPetSpecies = "Dog";
        NewPetBreed = string.Empty;
        NewPetAge = string.Empty;
        NewPetWeightKg = null;
        NewPetDiet = string.Empty;
    }

    // =========================================================
    // SAVE
    // =========================================================

    public async Task SaveNewPetAsync()
    {
        if (IsSaving)
            return;

        if (string.IsNullOrWhiteSpace(NewPetName))
        {
            IsError = true;
            StatusMessage = "Please enter a pet name.";
            return;
        }

        IsSaving = true;
        StatusMessage = string.Empty;
        IsError = false;

        try
        {
            var pet = new PetProfile
            {
                UserId = Auth.CurrentUser!.Id,
                PetName = NewPetName.Trim(),
                Species = NewPetSpecies,
                Breed = NewPetBreed?.Trim() ?? string.Empty,
                Age = NewPetAge?.Trim() ?? string.Empty,
                WeightKg = NewPetWeightKg,
                Diet = NewPetDiet?.Trim() ?? string.Empty
            };

            int newId = Db.AddPet(pet);

            if (newId <= 0)
            {
                IsError = true;
                StatusMessage = "Could not save the pet. Please try again.";
                return;
            }

            StatusMessage = $"<i class=\"bi bi-check-circle-fill\"></i> {pet.PetName} added successfully!";
            HideAddForm();
            Load();
        }
        catch (InvalidOperationException ex)
        {
            IsError = true;
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }

        await Task.CompletedTask;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task DeletePetAsync(PetProfile pet)
    {
        StatusMessage = string.Empty;
        IsError = false;

        bool ok = Db.DeletePet(pet.Id, Auth.CurrentUser!.Id);

        if (!ok)
        {
            IsError = true;
            StatusMessage = $"Could not delete {pet.PetName}.";
            return;
        }

        StatusMessage = $"🗑️ {pet.PetName} deleted.";
        Load();

        await Task.CompletedTask;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public string GetPetPhoto(int petId) => $"/uploads/pets/{petId}.jpg";
}