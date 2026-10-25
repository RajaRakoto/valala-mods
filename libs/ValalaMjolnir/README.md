> Cette bibliothèque est fournie par **Valala Mods** sous `ValalaMjolnir/`.

# ValalaMjolnir

Un marteau physique imprégné de foudre que vous pouvez forger, lancer, rappeler et
manier pour déchaîner des pouvoirs de tempête. Il reste à l'endroit où il atterrit,
et son marqueur de carte ainsi que sa flèche à l'écran vous aident à le retrouver.

## Caractéristiques du marteau

- **Lancer chargé :** maintenez le bouton de lancer pour charger. Les charges plus
  longues vont plus vite et plus loin.
- **Impacts physiques :** une fois lancé, Mjolnir tombe naturellement et endommage
  les ennemis, les structures et les objets qu'il touche. Les impacts au sol
  laissent des cratères dont la taille dépend de la force de l'impact.
- **Atterrissage et récupération persistants :** le marteau reste à l'endroit où il
  atterrit jusqu'à son rappel. Au-delà de 900 mètres, la récupération automatique le
  ramène.
- **Suivi :** un marqueur de carte et une flèche directionnelle à l'écran indiquent
  sa position.
- **Contrôle à distance :** guidez le marteau lancé vers votre réticule et faites-le
  léviter à cet endroit.
- **Foudre et vol :** visez la foudre sur un emplacement ou sur le marteau ;
  propulsez-vous dans les airs, planez et effectuez un écrasement au sol.

## Pouvoirs plus récents

- **Mode Sentinelle (L) :** Mjolnir flotte près de vous, éclaire la zone, dissipe
  la brume proche et foudroie périodiquement les ennemis qui approchent.
  Rappelez-le avec L ou H.
- **Orbite du Dieu du Tonnerre (appui bref sur Alt+L) :** des marteaux spectraux
  orbitent et plongent sur les ennemis proches, puis convergent pour une frappe
  finale.
- **Tempête de clones (maintenir Alt+L) :** manifestez et lancez une formation de
  marteaux spectraux, en terminant par une frappe de marteau. La dignité détermine
  le nombre maximal ; l'Eitr paie chaque manifestation. Maintenir au-delà de la
  limite peut déclencher une surcharge facultative alimentée par l'Eitr.
- **Progression avec Hugin :** Hugin présente les pouvoirs de marteau spectral à
  mesure que votre dignité et votre nombre de manifestations augmentent. Les
  tutoriels sont sauvegardés sur votre personnage.
- **Interface localisée :** le texte de l'objet, les pouvoirs, l'affichage tête
  haute (HUD), les dialogues de Hugin et les réglages suivent la langue
  sélectionnée dans Valheim lorsqu'une traduction est disponible.

## Installation et prérequis

Nécessite BepInExPack Valheim **5.4.2351** et Jotunn **2.30.2**. Installez avec un
gestionnaire de mods compatible Thunderstore. Pour une installation manuelle,
placez les deux DLL incluses dans :

```text
BepInEx/plugins/RajaRakoto-ValalaMods/ValalaMjolnir/ValheimMjolnir.dll
BepInEx/plugins/RajaRakoto-ValalaMods/ValalaMjolnir/ValheimMjolnir.Core.dll
```

Lancez Valheim une fois pour générer le fichier de configuration, puis fabriquez
Mjolnir. Cette version a été compilée pour Valheim 0.221.12 ; la compatibilité
avec des versions plus récentes du jeu et des dépendances nécessite une
vérification en jeu.

## Fabrication

Valala Mods déplace Mjolnir vers une forge noire de niveau 6 avec une recette de
fin de partie. Consultez le README de Valala Mods pour la liste actuelle des
matériaux.

## Touches

| Touche par défaut | Action |
| --- | --- |
| H | Charger et lancer Mjolnir, ou le rappeler lorsqu'il est au loin |
| G | Viser et activer la foudre ; écrasement au sol en vol |
| Y | Maintenir pour voler lorsque Mjolnir est équipé |
| J | Guider et faire léviter le marteau lancé |
| B | Écrasement vertical depuis le sol |
| L | Mode Sentinelle |

### Pouvoirs Alt contextuels

Le modificateur de pouvoir est **Alt gauche** par défaut. Une combinaison change
selon l'état actuel du marteau : **Alt+G** est Coup de tonnerre en main, Foudre
en chaîne lorsqu'il est au loin, et Lance de tonnerre en vol.

| État | Touche | Pouvoir | Action |
| --- | --- | --- | --- |
| En main | Alt+G | Coup de tonnerre | Charger puis relâcher une onde de choc |
| En main | Alt+H | Ligne d'orage | Foudre au sol le long de votre visée |
| En main | Alt+J | Piliers de foudre | Anneau de foudre à l'emplacement visé |
| En main | Alt+B | Invocation de la tempête | Invoquer une tempête au-dessus de votre personnage |
| Au loin | Alt+G | Foudre en chaîne | Enchaîner les ennemis proches |
| Au loin | Alt+H | Tempête de Mjolnir | Basculer les attaques sur les ennemis proches |
| Au loin | Alt+J | Barrage multi-angle | Attaquer depuis plusieurs directions |
| En vol | Alt+G | Lance de tonnerre | Traverser le réticule sous forme d'éclair |
| En vol | Alt+H | Charge de marteau volant | Foncer dans la direction de votre visée |
| Équipé | Alt+Y | Dieu du Tonnerre | Dépenser la foudre stockée pour renforcer Mjolnir |
| Sentinelle | Appui bref sur Alt+L | Orbite du Dieu du Tonnerre | Attaques orbitales et plongeantes spectrales |
| Sentinelle | Maintenir Alt+L | Tempête de clones | Manifester et lancer des marteaux spectraux |

Des pouvoirs supplémentaires sont disponibles.

| Touche | Pouvoir | Utilisation |
| --- | --- | --- |
| Alt+2 | Déflagration divine | Maintenir avec Mjolnir équipé ; relâcher après la charge |
| Alt+3 | Paratonnerre ancré | Utiliser après que le marteau lancé a atterri sur un sol statique |
| Alt+5 | Pas de foudre | Activer avec Mjolnir équipé |
| Alt+6 | Garde de Mjolnir | Maintenir avec Mjolnir équipé |

## Lancer, rappel et contrôle à distance

Maintenez **H** avec Mjolnir en main et relâchez pour le lancer. Appuyez sur **H**
lorsqu'il est au loin pour un rappel évitant les obstacles ; appuyez à nouveau
pendant le rappel pour l'annuler. Maintenez **H** pour un rappel forcé direct.
**Le rappel forcé peut détruire les structures et les objets du décor sur son
passage**, y compris près de votre base.

Après le lancer, appuyez sur **J** ou maintenez-le pour guider le marteau vers
votre réticule. Il lévite à l'endroit sélectionné. Relâchez **J** pour laisser la
gravité agir ; appuyez sur **H** pour le rappeler.

## Foudre, vol et renforcement par la tempête

Appuyez ou maintenez **G** pour viser, puis relâchez pour frapper. En main, la
frappe tombe sur votre visée. Lorsque Mjolnir est au loin, elle se centre sur le
marteau. En vol, **G** déclenche à la place un écrasement au sol.

Maintenez **Y** avec Mjolnir équipé pour voler dans la direction de la caméra.
Relâchez pour planer et descendre. Le vol protège contre les dégâts de chute
normaux si vous atterrissez pendant celui-ci.

La pluie, les tempêtes et Dieu du Tonnerre renforcent Mjolnir. Pendant le
renforcement, les dégâts du marteau sont doublés, les coûts d'endurance et les
temps de recharge des pouvoirs sont réduits de moitié, et les dégâts subis sont
réduits de moitié. Ces effets ne se cumulent pas. Une icône facultative près de
la mini-carte indique la source du renforcement.

**Dieu du Tonnerre (Alt+Y)** nécessite la dignité Approuvé par Thor, dépense la
foudre stockée, dure 10 secondes et frappe automatiquement les ennemis proches
en consommant de l'endurance. **Invocation de la tempête (Alt+B)** nécessite la
dignité Porteur de Tempête, de la foudre stockée et de l'endurance. Sa durée par
défaut est de 60 secondes. La météo invoquée est locale, n'est pas sauvegardée et
peut être remplacée par l'environnement de Valheim ou la météo de raid.

La foudre stockée alimente les deux capacités. Porter Mjolnir génère une charge
toutes les trois secondes ; la pluie naturelle le charge plus vite lorsque Mjolnir
est sorti et à découvert. Une tempête invoquée ne recharge pas la réserve utilisée
pour l'appeler.

## Dignité et réglages

Mjolnir se lie à votre personnage. La dignité peut augmenter les dégâts du marteau
jusqu'à 30 % ; les joueurs jugés indignes ne peuvent pas le soulever. Des actions
comme vaincre un gardien, protéger autrui ou affronter des ennemis plus forts
augmentent la dignité. Blesser des créatures sans défense et mourir peuvent la
faire baisser. Les règles de dignité, les touches et les réglages du mod restent
configurables.

Les variations de dignité par défaut comprennent :

| Action | Dignité |
| --- | ---: |
| Première victoire contre chaque gardien | +10 |
| Protéger une créature apprivoisée ou un autre joueur | +3 |
| Vaincre un ennemi plus fort | +1,5 |
| Gagner un combat au bord de la mort | +1 |
| Épargner un ennemi en fuite | +0,25 |
| Vaincre un ennemi bien plus faible | -0,05 |
| Frapper un ennemi en fuite | -0,3 |
| Tuer une créature apprivoisée ou un autre joueur | -2,5 |
| Mourir en combattant à pied | Jusqu'à -0,75 |
| Mourir pendant un pouvoir | Jusqu'à -6 |

Les pénalités de mort augmentent avec les morts répétées ; la première mort après
une pause coûte moins cher. Chasser la faune passive est généralement neutre ou
entraîne une petite pénalité. Les paliers de dignité commencent à 15 (Réceptif),
35 (Porteur de Tonnerre), 60 (Porteur de Tempête) et 85 (Approuvé par Thor). Le
rythme de progression dépend du style de jeu ; les règles restent configurables.

Tant que Mjolnir est tenu, les coûts d'endurance sont réduits de 50 %, et les
étourdissements normaux ainsi que le recul infligé par les ennemis sont annulés.

## Langues

Le mod inclut l'anglais, le français, l'allemand, l'italien, l'espagnol, le
japonais, le coréen, le russe, le chinois traditionnel et le polonais. Les autres
langues du jeu utilisent l'anglais.
