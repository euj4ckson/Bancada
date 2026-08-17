using Bancada.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Infrastructure;

public sealed class DevelopmentDataSeeder(BancadaDbContext dbContext, UserManager<ApplicationUser> userManager)
{
    private const string CheeseBreadCoverImageUrl = "/images/recipes/pao-de-queijo-tabuleiro.webp";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Recipes.AnyAsync(cancellationToken))
        {
            await RepairSeededRecipeImagesAsync(cancellationToken);
            return;
        }

        var users = await CreateUsersAsync();
        var now = DateTimeOffset.UtcNow;

        var recipes = new[]
        {
            new SeedRecipe("Nhoque de mandioquinha com manteiga de sálvia", "Macio, dourado na frigideira e terminado com sálvia fresca.", "Cozinhe a mandioquinha e passe ainda quente. Misture a farinha aos poucos, modele e corte. Cozinhe até subir à superfície. Doure a manteiga com a sálvia e envolva o nhoque.", 70, RecipeDifficulty.Medium, 4, users[0], "https://images.unsplash.com/photo-1551183053-bf91a1d81141?auto=format&fit=crop&w=1200&q=82", new[] { ("Mandioquinha", "700", "g"), ("Farinha de trigo", "1", "xícara"), ("Manteiga", "80", "g"), ("Sálvia", "12", "folhas") }),
            new SeedRecipe("Frango assado com limão e alho", "Frango de forno com pele dourada, alho macio e bastante limão.", "Tempere o frango com sal, alho, raspas e suco de limão. Deixe descansar por 30 minutos. Asse em forno alto até dourar, regando com o próprio caldo na metade do tempo.", 80, RecipeDifficulty.Easy, 5, users[1], "https://images.unsplash.com/photo-1532550907401-a500c9a57435?auto=format&fit=crop&w=1200&q=82", new[] { ("Coxa e sobrecoxa", "1,2", "kg"), ("Limão-siciliano", "2", "unidades"), ("Alho", "6", "dentes"), ("Alecrim", "3", "ramos") }),
            new SeedRecipe("Bolo de fubá com goiabada", "Bolo de miolo úmido com cubos de goiabada que ficam espalhados pela massa.", "Bata ovos, açúcar, leite e óleo. Junte fubá, farinha e fermento. Passe a goiabada em um pouco de farinha, distribua sobre a massa e asse até o palito sair limpo.", 55, RecipeDifficulty.Easy, 10, users[2], "https://images.unsplash.com/photo-1578985545062-69928b1d9587?auto=format&fit=crop&w=1200&q=82", new[] { ("Fubá", "2", "xícaras"), ("Goiabada", "180", "g"), ("Ovo", "3", "unidades"), ("Leite", "1", "xícara") }),
            new SeedRecipe("Risoto de cogumelos e alho assado", "Cremoso sem excesso de manteiga, com cogumelos bem dourados.", "Asse o alho embrulhado até ficar macio. Doure os cogumelos em fogo alto. Refogue o arroz, adicione o caldo aos poucos e mexa. Finalize com alho assado, cogumelos, queijo e manteiga.", 50, RecipeDifficulty.Medium, 4, users[0], "https://images.unsplash.com/photo-1476124369491-e7addf5db371?auto=format&fit=crop&w=1200&q=82", new[] { ("Arroz arbóreo", "320", "g"), ("Cogumelo paris", "250", "g"), ("Alho", "1", "cabeça"), ("Caldo de legumes", "1,2", "litro") }),
            new SeedRecipe("Pão de queijo de tabuleiro", "Casquinha firme, centro elástico e preparo sem modelar bolinhas.", "Escalde o polvilho com leite, óleo e sal. Espere amornar, misture ovos e queijo. Espalhe em assadeira untada e asse até crescer e dourar.", 45, RecipeDifficulty.Easy, 12, users[1], CheeseBreadCoverImageUrl, new[] { ("Polvilho azedo", "400", "g"), ("Queijo meia cura", "250", "g"), ("Leite", "200", "ml"), ("Ovo", "3", "unidades") }),
            new SeedRecipe("Moqueca de banana-da-terra", "Uma moqueca vegetal encorpada, com pimentões e leite de coco.", "Doure rapidamente as bananas. Monte camadas com cebola, tomate e pimentões. Acrescente leite de coco e dendê, tampe e cozinhe em fogo baixo. Finalize com coentro e limão.", 40, RecipeDifficulty.Easy, 4, users[2], "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=1200&q=82", new[] { ("Banana-da-terra", "4", "unidades"), ("Leite de coco", "400", "ml"), ("Pimentão vermelho", "1", "unidade"), ("Azeite de dendê", "2", "colheres") }),
            new SeedRecipe("Arroz de forno com abóbora e queijo", "Travessa prática para aproveitar arroz pronto e pedaços de abóbora assada.", "Misture o arroz com abóbora, ervas e metade do queijo. Coloque em uma travessa, cubra com o restante do queijo e leve ao forno até borbulhar e dourar.", 35, RecipeDifficulty.Easy, 6, users[0], "https://images.unsplash.com/photo-1512058564366-18510be2db19?auto=format&fit=crop&w=1200&q=82", new[] { ("Arroz cozido", "4", "xícaras"), ("Abóbora cabotiá", "500", "g"), ("Queijo muçarela", "200", "g"), ("Salsinha", "1", "punhado") }),
            new SeedRecipe("Peixe grelhado com vinagrete de feijão-fradinho", "Peixe suculento com um acompanhamento fresco e levemente ácido.", "Cozinhe o feijão até ficar macio sem desmanchar. Misture com tomate, cebola, cheiro-verde e limão. Tempere o peixe e grelhe em frigideira bem quente.", 45, RecipeDifficulty.Medium, 4, users[1], "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?auto=format&fit=crop&w=1200&q=82", new[] { ("Filé de peixe branco", "600", "g"), ("Feijão-fradinho", "1", "xícara"), ("Tomate", "2", "unidades"), ("Limão", "1", "unidade") }),
            new SeedRecipe("Torta rústica de tomate", "Massa amanteigada aberta, recheada com tomates bem temperados.", "Faça uma massa com farinha, manteiga e água gelada. Descanse, abra e cubra com tomates sem chegar às bordas. Dobre as bordas para dentro e asse até ficar crocante.", 75, RecipeDifficulty.Medium, 6, users[2], "https://images.unsplash.com/photo-1572449043416-55f4685c9bb7?auto=format&fit=crop&w=1200&q=82", new[] { ("Tomate", "5", "unidades"), ("Farinha de trigo", "2", "xícaras"), ("Manteiga", "120", "g"), ("Mostarda", "1", "colher") }),
            new SeedRecipe("Cuscuz paulista de legumes", "Uma versão colorida com milho, ervilha, palmito e bastante cheiro-verde.", "Refogue os legumes e junte o caldo. Acrescente a farinha de milho aos poucos até soltar da panela. Disponha a decoração na forma, preencha com a massa e desenforme morno.", 50, RecipeDifficulty.Medium, 8, users[0], "https://images.unsplash.com/photo-1592415486689-125cbbfcbee2?auto=format&fit=crop&w=1200&q=82", new[] { ("Farinha de milho flocada", "3", "xícaras"), ("Palmito", "200", "g"), ("Milho-verde", "1", "lata"), ("Ervilha", "1", "xícara") })
        };

        foreach (var item in recipes)
        {
            var recipe = new Recipe(item.Author.Id, item.Title, item.Description, item.Instructions,
                item.Minutes, item.Difficulty, item.Servings, now.AddDays(-recipes.Length));
            recipe.SetCoverImage(item.CoverImageUrl, now);

            var order = 0;
            foreach (var (name, quantity, unit) in item.Ingredients)
            {
                var ingredient = await FindOrCreateIngredientAsync(name, cancellationToken);
                recipe.Ingredients.Add(new RecipeIngredient(recipe.Id, ingredient.Id, quantity, unit, null, order++));
            }

            recipe.Publish(now.AddDays(-order));
            dbContext.Recipes.Add(recipe);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await CreateChallengesAsync(now, cancellationToken);
    }

    private async Task RepairSeededRecipeImagesAsync(CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes.SingleOrDefaultAsync(
            item => item.Title == "Pão de queijo de tabuleiro",
            cancellationToken);

        if (recipe is null || string.Equals(recipe.CoverImageUrl, CheeseBreadCoverImageUrl, StringComparison.Ordinal))
        {
            return;
        }

        recipe.SetCoverImage(CheeseBreadCoverImageUrl, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser[]> CreateUsersAsync()
    {
        var users = new[]
        {
            new ApplicationUser { UserName = "mariana@bancada.local", Email = "mariana@bancada.local", DisplayName = "Mariana Lopes", Bio = "Cozinho devagar e sempre faço caldo a mais.", CreatedAt = DateTimeOffset.UtcNow.AddMonths(-8) },
            new ApplicationUser { UserName = "caio@bancada.local", Email = "caio@bancada.local", DisplayName = "Caio Nascimento", Bio = "Receitas de forno, feira e fim de semana.", CreatedAt = DateTimeOffset.UtcNow.AddMonths(-5) },
            new ApplicationUser { UserName = "luciana@bancada.local", Email = "luciana@bancada.local", DisplayName = "Luciana Prado", Bio = "Doces brasileiros e comida sem desperdício.", CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3) }
        };

        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user, "Bancada123!");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        return users;
    }

    private async Task CreateChallengesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = new Challenge("Até a última migalha", "Crie um prato em que pão amanhecido tenha papel principal.", now.AddDays(-2), now.AddDays(5), ChallengeStatus.Active,
            "https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=1400&q=82");
        var closed = new Challenge("Cores da feira", "Monte um prato usando ingredientes de pelo menos três cores naturais.", now.AddDays(-18), now.AddDays(-8), ChallengeStatus.Closed,
            "https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=1400&q=82");

        foreach (var (challenge, ingredients) in new[]
                 {
                     (active, new[] { "Pão amanhecido" }),
                     (closed, new[] { "Tomate", "Cenoura", "Folhas verdes" })
                 })
        {
            foreach (var name in ingredients)
            {
                var ingredient = await FindOrCreateIngredientAsync(name, cancellationToken);
                challenge.Ingredients.Add(new ChallengeIngredient(challenge.Id, ingredient.Id, true));
            }

            dbContext.Challenges.Add(challenge);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ingredient> FindOrCreateIngredientAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = Ingredient.Normalize(name);
        var tracked = dbContext.Ingredients.Local.FirstOrDefault(ingredient => ingredient.NormalizedName == normalized);
        if (tracked is not null)
        {
            return tracked;
        }

        var existing = await dbContext.Ingredients.FirstOrDefaultAsync(ingredient => ingredient.NormalizedName == normalized, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var ingredient = new Ingredient(name);
        dbContext.Ingredients.Add(ingredient);
        return ingredient;
    }

    private sealed record SeedRecipe(string Title, string Description, string Instructions, int Minutes,
        RecipeDifficulty Difficulty, int Servings, ApplicationUser Author, string CoverImageUrl,
        IReadOnlyList<(string Name, string Quantity, string Unit)> Ingredients);
}
