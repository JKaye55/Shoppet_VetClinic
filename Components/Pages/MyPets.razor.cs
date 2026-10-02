using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shoppet_VetClinic.Models;
using Shoppet_VetClinic.Services;

namespace Shoppet_VetClinic.Components.Pages;

public partial class MyPets
{
    // =========================================================
    // SERVICES
    // =========================================================

    [Inject]
    private DatabaseService Db { get; set; }
        = default!;


    [Inject]
    private AuthService Auth { get; set; }
        = default!;


    [Inject]
    private FileStorageService FileStorage { get; set; }
        = default!;


    [Inject]
    private NavigationManager Nav { get; set; }
        = default!;


    // =========================================================
    // PAGE STATE
    // =========================================================

    public List<PetProfile> Pets { get; private set; }
        = new();


    public bool IsPremium { get; private set; }

    public bool CanAddMorePets { get; private set; }

    public bool ShowPetForm { get; private set; }

    public bool IsEditingPet { get; private set; }

    public bool IsSaving { get; private set; }


    public string StatusMessage { get; private set; }
        = string.Empty;


    public bool IsError { get; private set; }


    // =========================================================
    // FORM
    // =========================================================

    public PetProfile PetForm { get; private set; }
        = CreateBlankPet();


    public IBrowserFile? SelectedPetFile { get; private set; }


    public string SelectedPetFileName { get; private set; }
        = string.Empty;


    public string PetPhotoPreview { get; private set; }
        = string.Empty;


    private bool _initialized;


    // =========================================================
    // BREED OPTIONS
    // =========================================================

    private readonly Dictionary<string, List<string>>
        _breedOptions =
            new()
            {
                ["Dog"] = new()
                {
                    "Shih Tzu",
                    "Golden Retriever",
                    "Labrador Retriever",
                    "Beagle",
                    "Pomeranian",
                    "Poodle",
                    "Chihuahua",
                    "German Shepherd",
                    "Siberian Husky",
                    "French Bulldog",
                    "Aspin / Mixed Breed",
                    "Other Dog Breed"
                },

                ["Cat"] = new()
                {
                    "Persian",
                    "Siamese",
                    "British Shorthair",
                    "Maine Coon",
                    "Ragdoll",
                    "Domestic Shorthair",
                    "Puspin / Mixed Breed",
                    "Other Cat Breed"
                },

                ["Rabbit"] = new()
                {
                    "Holland Lop",
                    "Lionhead",
                    "Netherland Dwarf",
                    "Mini Rex",
                    "Mixed Breed Rabbit",
                    "Other Rabbit Breed"
                },

                ["Bird"] = new()
                {
                    "Lovebird",
                    "Parakeet",
                    "Cockatiel",
                    "African Grey",
                    "Canary",
                    "Other Bird"
                },

                ["Hamster"] = new()
                {
                    "Syrian Hamster",
                    "Dwarf Hamster",
                    "Roborovski Hamster",
                    "Chinese Hamster",
                    "Other Hamster"
                },

                ["Fish"] = new()
                {
                    "Betta",
                    "Goldfish",
                    "Guppy",
                    "Koi",
                    "Molly",
                    "Other Fish"
                },

                ["Other"] = new()
            };


    public IEnumerable<string> CurrentBreeds
    {
        get
        {
            if (_breedOptions.TryGetValue(
                PetForm.Species ?? "Dog",
                out var breeds))
            {
                return breeds;
            }


            return Array.Empty<string>();
        }
    }


    // =========================================================
    // AGE OPTIONS
    // =========================================================

    public IReadOnlyList<string> AgeOptions { get; }
        =
        new List<string>
        {
            "Under 1 year",
            "1 year",
            "2 years",
            "3 years",
            "4 years",
            "5 years",
            "6 years",
            "7 years",
            "8 years",
            "9 years",
            "10 years",
            "11 years",
            "12 years",
            "13 years",
            "14 years",
            "15+ years",
            "Unknown"
        };


    // =========================================================
    // DIET OPTIONS
    // =========================================================

    public IReadOnlyList<string> DietOptions { get; }
        =
        new List<string>
        {
            "Dry Food",
            "Wet Food",
            "Mixed Dry & Wet Food",
            "Home-Prepared Food",
            "Raw / Fresh Diet",
            "Veterinary / Prescription Diet",
            "Other"
        };


    // =========================================================
    // INITIALIZATION
    // =========================================================

    protected override async Task OnParametersSetAsync()
    {
        await Auth.InitializeAsync();


        if (_initialized)
            return;


        _initialized =
            true;


        Load();
    }


    // =========================================================
    // LOAD
    // =========================================================

    private void Load()
    {
        if (!Auth.IsLoggedIn ||
            Auth.CurrentUser is null)
        {
            Nav.NavigateTo(
                "/login");


            return;
        }


        if (!Auth.IsPetOwner &&
            !Auth.IsAdmin)
        {
            Nav.NavigateTo(
                "/dashboard");


            return;
        }


        Pets =
            Db.GetPetsByUser(
                Auth.CurrentUser.Id)
            ??
            new List<PetProfile>();


        IsPremium =
      Db.IsUserPremium(
          Auth.CurrentUser.Id);


        CanAddMorePets =
            IsPremium
            ||
            Pets.Count < 1;
    }


    // =========================================================
    // ADD
    // =========================================================

    public void OpenAddForm()
    {
        if (!CanAddMorePets)
        {
            ShowError(
                "Your Free plan already includes one pet. Upgrade to Premium to add more pets.");


            return;
        }


        ResetForm();


        PetForm =
            CreateBlankPet();


        IsEditingPet =
            false;


        ShowPetForm =
            true;


        StatusMessage =
            string.Empty;


        IsError =
            false;
    }


    // =========================================================
    // EDIT
    // =========================================================

    public void OpenEditForm(
        PetProfile pet)
    {
        if (Auth.CurrentUser is null ||
            pet.UserId != Auth.CurrentUser.Id)
        {
            ShowError(
                "You do not have permission to edit this pet.");


            return;
        }


        SelectedPetFile =
            null;


        SelectedPetFileName =
            string.Empty;


        PetForm =
            new PetProfile
            {
                Id =
                    pet.Id,

                UserId =
                    pet.UserId,

                PetName =
                    pet.PetName,

                Breed =
                    pet.Breed,

                Species =
                    string.IsNullOrWhiteSpace(
                        pet.Species)
                        ? "Dog"
                        : pet.Species,

                Age =
                    pet.Age,
                BirthDate =
    pet.BirthDate,

                WeightKg =
                    pet.WeightKg,

                Diet =
                    pet.Diet,

                CreatedAt =
                    pet.CreatedAt,

                CardId =
                    pet.CardId,

                CardIssuedAt =
                    pet.CardIssuedAt,

                CardTheme =
                    pet.CardTheme
            };


        PetPhotoPreview =
            FileStorage.GetImageUrl(
                "pets",
                pet.Id)
            ??
            string.Empty;


        IsEditingPet =
            true;


        ShowPetForm =
            true;


        StatusMessage =
            string.Empty;


        IsError =
            false;
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void ClosePetForm()
    {
        if (IsSaving)
            return;


        ShowPetForm =
            false;


        IsEditingPet =
            false;


        ResetForm();
    }

    public void OnBirthDateChanged()
    {
        if (PetForm.BirthDate.HasValue)
        {
            if (PetForm.BirthDate.Value > DateTime.Today)
            {
                PetForm.BirthDate = DateTime.Today;
                ShowError("Birthdate cannot be a future date.");
            }

            var today = DateTime.Today;
            var birthDate = PetForm.BirthDate.Value;

            if (birthDate > today) return;

            int years = today.Year - birthDate.Year;
            int months = today.Month - birthDate.Month;
            if (today.Day < birthDate.Day)
            {
                months--;
            }
            if (months < 0)
            {
                years--;
                months += 12;
            }

            if (years <= 0 && months <= 0)
            {
                PetForm.Age = "Under 1 year";
            }
            else if (years <= 0)
            {
                PetForm.Age = $"{months} month{(months == 1 ? "" : "s")}";
            }
            else if (months <= 0)
            {
                PetForm.Age = $"{years} year{(years == 1 ? "" : "s")}";
            }
            else
            {
                PetForm.Age = $"{years} year{(years == 1 ? "" : "s")}, {months} month{(months == 1 ? "" : "s")}";
            }
        }
    }


    // =========================================================
    // SPECIES → BREED
    // =========================================================

    public void SpeciesChanged()
    {
        PetForm.Breed =
            string.Empty;
    }


    // =========================================================
    // PHOTO
    // =========================================================

    public async Task HandlePetPhotoSelected(
        InputFileChangeEventArgs e)
    {
        var file =
            e.File;


        if (!IsValidImage(file))
        {
            SelectedPetFile =
                null;


            SelectedPetFileName =
                string.Empty;


            ShowError(
                "Please choose a JPG, PNG, or WEBP image up to 5 MB.");


            return;
        }


        try
        {
            SelectedPetFile =
                file;


            SelectedPetFileName =
                file.Name;


            PetPhotoPreview =
                await CreatePreviewAsync(
                    file);


            StatusMessage =
                string.Empty;


            IsError =
                false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Pet Photo Preview] {ex}");


            SelectedPetFile =
                null;


            SelectedPetFileName =
                string.Empty;


            ShowError(
                "The selected image could not be opened.");
        }
    }


    // =========================================================
    // SAVE
    // =========================================================

    public async Task SavePetAsync()
    {
        if (IsSaving)
            return;

        if (PetForm.BirthDate.HasValue &&
    PetForm.BirthDate.Value.Date >
    DateTime.Today)
        {
            ShowError(
                "Pet birthdate cannot be in the future.");

            return;
        }

        if (Auth.CurrentUser is null)
        {
            ShowError(
                "Your session has expired. Please sign in again.");


            return;
        }


        PetForm.PetName =
            PetForm.PetName?
                .Trim()
            ??
            string.Empty;


        PetForm.Breed =
            PetForm.Breed?
                .Trim()
            ??
            string.Empty;


        PetForm.Age =
            PetForm.Age?
                .Trim()
            ??
            string.Empty;


        PetForm.Diet =
            PetForm.Diet?
                .Trim()
            ??
            string.Empty;


        if (string.IsNullOrWhiteSpace(
            PetForm.PetName))
        {
            ShowError(
                "Please enter your pet's name.");


            return;
        }


        if (string.IsNullOrWhiteSpace(
            PetForm.Species))
        {
            ShowError(
                "Please select your pet's species.");


            return;
        }


        if (PetForm.WeightKg.HasValue &&
            PetForm.WeightKg.Value < 0)
        {
            ShowError(
                "Pet weight cannot be negative.");


            return;
        }


        IsSaving =
            true;


        StatusMessage =
            string.Empty;


        IsError =
            false;


        var editing =
            IsEditingPet;


        try
        {
            PetForm.UserId =
                Auth.CurrentUser.Id;


            int petId;


            // =================================================
            // UPDATE
            // =================================================

            if (editing)
            {
                var updated =
                    Db.UpdatePet(
                        PetForm);


                if (!updated)
                {
                    ShowError(
                        "The pet profile could not be updated.");


                    return;
                }


                petId =
                    PetForm.Id;
            }

            // =================================================
            // CREATE
            // =================================================

            else
            {
                if (!CanAddMorePets)
                {
                    ShowError(
                        "Your Free plan already includes one pet.");


                    return;
                }


                petId =
                    Db.AddPet(
                        PetForm);


                if (petId <= 0)
                {
                    ShowError(
                        "The pet profile could not be created.");


                    return;
                }
            }


            // =================================================
            // PHOTO
            // =================================================

            var photoFailed =
                false;


            if (SelectedPetFile is not null)
            {
                try
                {
                    await FileStorage
                        .SaveImageAsync(
                            "pets",
                            petId,
                            SelectedPetFile);
                }
                catch (Exception ex)
                {
                    photoFailed =
                        true;


                    Console.WriteLine(
                        $"[Pet Photo Save] {ex}");
                }
            }


            var petName =
                PetForm.PetName;


            ShowPetForm =
                false;


            IsEditingPet =
                false;


            ResetForm();


            Load();


            if (photoFailed)
            {
                ShowError(
                    $"{petName}'s information was saved, but the photo could not be uploaded.");
            }
            else
            {
                ShowSuccess(
                    editing
                        ? $"{petName}'s profile was updated."
                        : $"{petName} was added successfully.");
            }
        }
        catch (InvalidOperationException ex)
        {
            ShowError(
                ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Save Pet] {ex}");


            ShowError(
                "The pet profile could not be saved. Please try again.");
        }
        finally
        {
            IsSaving =
                false;
        }
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task DeletePetAsync(
        PetProfile pet)
    {
        if (Auth.CurrentUser is null)
            return;


        try
        {
            var deleted =
                Db.DeletePet(
                    pet.Id,
                    Auth.CurrentUser.Id);


            if (!deleted)
            {
                ShowError(
                    $"{pet.PetName} could not be deleted.");


                return;
            }


            try
            {
                FileStorage.DeleteImage(
                    "pets",
                    pet.Id);
            }
            catch
            {
                // Database deletion should not fail just because
                // an image was already missing.
            }


            Load();


            ShowSuccess(
                $"{pet.PetName} was deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Delete Pet] {ex}");


            ShowError(
                $"{pet.PetName} could not be deleted. The pet may still have related records.");
        }


        await Task.CompletedTask;
    }


    // =========================================================
    // PHOTO LOOKUP
    // =========================================================

    public string? GetPetPhoto(
        int petId)
    {
        return
            FileStorage.GetImageUrl(
                "pets",
                petId);
    }


    // =========================================================
    // IMAGE VALIDATION
    // =========================================================

    private static bool IsValidImage(
        IBrowserFile file)
    {
        const long maxSize =
            5 * 1024 * 1024;


        var allowedTypes =
            new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };


        return
            file.Size > 0
            &&
            file.Size <= maxSize
            &&
            allowedTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase);
    }


    private static async Task<string>
        CreatePreviewAsync(
            IBrowserFile file)
    {
        const long maxSize =
            5 * 1024 * 1024;


        await using var stream =
            file.OpenReadStream(
                maxSize);


        using var memory =
            new MemoryStream();


        await stream.CopyToAsync(
            memory);


        return
            $"data:{file.ContentType};base64," +
            Convert.ToBase64String(
                memory.ToArray());
    }


    // =========================================================
    // FORM RESET
    // =========================================================

    private void ResetForm()
    {
        PetForm =
            CreateBlankPet();


        SelectedPetFile =
            null;


        SelectedPetFileName =
            string.Empty;


        PetPhotoPreview =
            string.Empty;
    }


    private static PetProfile CreateBlankPet()
    {
        return new PetProfile
        {
            Species =
                "Dog",

            Breed =
                string.Empty,

            Age =
                string.Empty,

            Diet =
                string.Empty
        };
    }

    // =========================================================
    // DISPLAY AGE
    // =========================================================

    public string GetDisplayAge(
        PetProfile pet)
    {
        /*
            BirthDate is the preferred source because it keeps
            the displayed age accurate automatically.

            The old Age field is retained as a fallback for
            existing pet profiles that do not yet have a
            BirthDate.
        */

        if (!pet.BirthDate.HasValue)
        {
            return
                string.IsNullOrWhiteSpace(
                    pet.Age)

                    ? "Not set"
                    : pet.Age;
        }


        var birthDate =
            pet.BirthDate.Value.Date;


        var today =
            DateTime.Today;


        if (birthDate > today)
        {
            return "Not set";
        }


        var years =
            today.Year -
            birthDate.Year;


        if (birthDate >
            today.AddYears(-years))
        {
            years--;
        }


        /*
            For pets under one year old, showing months is much
            more useful than simply displaying "0 years".
        */

        if (years <= 0)
        {
            var months =
                (
                    (today.Year - birthDate.Year) * 12
                )
                +
                today.Month
                -
                birthDate.Month;


            if (today.Day < birthDate.Day)
            {
                months--;
            }


            months =
                Math.Max(
                    0,
                    months);


            if (months == 0)
            {
                var days =
                    Math.Max(
                        0,
                        (today - birthDate).Days);


                return
                    days == 1

                        ? "1 day"
                        : $"{days} days";
            }


            return
                months == 1

                    ? "1 month"
                    : $"{months} months";
        }


        return
            years == 1

                ? "1 year"
                : $"{years} years";
    }

    // =========================================================
    // INITIAL
    // =========================================================

    public string GetPetInitial(
        PetProfile pet)
    {
        if (string.IsNullOrWhiteSpace(
            pet.PetName))
        {
            return "P";
        }


        return
            pet.PetName
                .Substring(0, 1)
                .ToUpperInvariant();
    }


    // =========================================================
    // MESSAGES
    // =========================================================

    private void ShowSuccess(
        string message)
    {
        StatusMessage =
            message;


        IsError =
            false;
    }


    private void ShowError(
        string message)
    {
        StatusMessage =
            message;


        IsError =
            true;
    }
}