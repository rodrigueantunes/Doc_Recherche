# Doc Recherche

## Description

Ce programme permet de rechercher des fichiers dans plusieurs dossiers et sous-dossiers, puis de filtrer les résultats en fonction de mots-clés spécifiés. Il fournit des fonctionnalités telles que l'ouverture du dossier contenant un fichier via un clic droit ou un bouton, et un affichage de la progression de la recherche.

### Fonctionnalités principales :
1. **Recherche de fichiers** : Recherche dans plusieurs dossiers spécifiés (et leurs sous-dossiers) pour les fichiers avec des extensions spécifiques (ex. `.html`, `.htm`, `.4gl`).
2. **Filtrage par mots-clés** : Filtrage des fichiers en fonction des mots-clés spécifiés. Les mots-clés sont utilisés pour rechercher des correspondances dans le contenu des fichiers.
3. **Affichage des résultats** : Affiche les fichiers trouvés dans une `ListBox` avec des informations détaillées.
4. **Affichage de la progression** : Une barre de progression et un pourcentage indiquent l'état de la recherche.
5. **Interaction avec les résultats** : 
   - **Clic droit** : Ouvrir le dossier contenant le fichier sélectionné via l'explorateur de fichiers.
   - **Bouton "Ouvrir Dossier"** : Effectue la même action que le clic droit, mais avec un bouton.
6. **Affichage d'erreurs** : Si un dossier est inaccessible ou s'il y a des erreurs de lecture de fichiers, un message d'avertissement est affiché.
7. **Gestion de l'interface utilisateur** : Un bouton de recherche qui change de texte et désactive certains champs pendant l'exécution de la recherche.

## Installation

1. Téléchargez la dernière Release
2. Exécutez le programme.

## Prérequis

- .NET Framework 4.7.2 ou supérieur.
- Windows (10/11).

## Utilisation

1. **Saisie des mots-clés** : Entrez un ou plusieurs mots-clés séparés par des virgules dans le champ "Mots-clés".
2. **Sélection des dossiers** : Entrez les chemins des dossiers à rechercher dans les champs "Dossier 1", "Dossier 2" et "Dossier 3". Vous pouvez laisser ces champs vides si nécessaire.
3. **Lancer la recherche** : Cliquez sur le bouton "Rechercher". La recherche s'effectue dans les dossiers spécifiés, et les fichiers trouvés sont affichés dans une `ListBox`.
4. **Afficher les résultats** : 
   - Les résultats sont mis à jour au fur et à mesure de l'exécution de la recherche.
   - Une barre de progression et un pourcentage indiquent la progression de la recherche.
5. **Interaction avec les résultats** :
   - **Clic droit** : Cliquez avec le bouton droit sur un fichier dans la `ListBox` pour ouvrir le dossier contenant ce fichier dans l'explorateur de fichiers.
   - **Bouton "Ouvrir Dossier"** : Sélectionnez un fichier et cliquez sur le bouton "Ouvrir Dossier" pour ouvrir le dossier dans l'explorateur.

## Fonctionnement interne

Le programme utilise plusieurs fonctionnalités de .NET pour rechercher des fichiers et filtrer leur contenu :

- **Recherche de fichiers** : Utilisation de `Directory.GetFiles` pour parcourir les dossiers et sous-dossiers.
- **Filtrage de contenu** : Lecture des fichiers et utilisation de `Regex` pour vérifier si le contenu du fichier correspond aux mots-clés spécifiés.
- **Gestion des erreurs** : Gestion des erreurs d'accès aux dossiers et des erreurs de lecture de fichiers via des blocs `try-catch`.
- **Threading parallèle** : Utilisation de `Parallel.ForEach` et `Task.Run` pour effectuer la recherche de manière asynchrone et non bloquante.

## Contribution

Si vous souhaitez contribuer à ce projet, veuillez suivre les étapes suivantes :

1. Fork ce repository.
2. Créez une branche pour votre fonctionnalité (`git checkout -b feature-nouvelle-fonctionnalité`).
3. Faites vos changements et validez-les (`git commit -am 'Ajout d'une nouvelle fonctionnalité'`).
4. Poussez vos changements (`git push origin feature-nouvelle-fonctionnalité`).
5. Ouvrez une pull request.

## Auteurs

- **Antunes-Barata Rodrigue** - _Développeur principal_ - [rodrigueantunes]([https://github.com/rodrigueantunes])

## License

Ce projet est sous la licence MIT 

