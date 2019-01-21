# Trophies

Trophies are awarded to players depending on actions taken during a game, such as playing certain types of cards.

## Trophy object format

A trophy object consists of several components. Below is some example JSON showing how they are structured:

```json
{
    // Internal ID string for trophy
    "id": "trophy_example",

    // Display name for trophy
    "name": {
        "en-US": "Kinky"
    },

    // Description displayed next to trophy
    "desc": {
        "en-US": "20% or more cards played feature unusual fetishes."
    },

    // Requirements for getting trophy
    "requirements": [
        {
            "type": "card_proportion",
            "flags": ["xf"],
            "winning": false,
            "percent": 20
        }
    ]
}
```

## Trophy requirements

Requirements define the conditions that must be met in order for a player to earn a specific trophy. If multiple requirements are listed, all must be met.

### Any

Type name: `any`

Requires that one or more of the specified requirements be met.

```json
{
    "type": "any",
    "requirements": [
        { 
            /* ... */
        },
        { 
            /* ... */
        }
    ]
}
```

### All

Type name: `all`

Requires that all of the specified requirements be met.

```json
{
    "type": "all",
    "requirements": [
        { 
            /* ... */
        },
        { 
            /* ... */
        }
    ]
}
```

### Card Proportion

Type name: `card_proportion`

Requires a certain percentage of played cards to match at least one of the specified content flag strings.

```json
{
    "type": "card_proportion",

    // Flag filters. Contents must include at least one of the array items.
    "flags": ["s", "v"], // Cards with either sexual content or violence

    "winning": false, // Specifies whether cards must have won a round

    // Minimum percentage of cards to be met
    "percent": 20
}
```

## Card contents

Card contents are identified using the content flags listed in the table below:

|Flag|Description|
|:--:|-----------|
|`a`|General abuse flag.|
|`an`|Animals.|
|`as`|Antisemitism or Nazism.|
|`bd`|BDSM.|
|`bg`|Blood/Gore.|
|`ce`|Celebrities.|
|`ch`|Children.|
|`cn`|Cannibalism.|
|`_cr`|(Postfix) Cruelty or unfair treatment.|
|`d`|Death.|
|`ds`|Disabilities.|
|`db`|Abortion/dead baby jokes.|
|`di`|Disease/Sickness/Medical conditions.|
|`dr`|Drugs/Drug usage.|
|`fc`|Fictional characters.|
|`fe`|Feces.|
|`g`|Generally regarded as disgusting.|
|`gn`|Genitalia.|
|`gnf`|Female genitalia.|
|`gnm`|Male genitalia.|
|`hp`|Homophobia/Transphobia.|
|`i`|Injury.|
|`lgbt`|Relating to the LGBT community.|
|`ma`|Misandry.|
|`mg`|Misogyny.|
|`mu`|Murder.|
|`ncs`|Nonconsensual sexual acts.|
|`nh`|Non-heterosexual sexual acts.|
|`pd`|Pedophilia.|
|`re`|Religion or religious people.|
|`s`|General sexual content flag.|
|`t`|Terrorism/Terroristic threats.|
|`v`|General violence flag.|
|`w`|Generally regarded as weird or unsettling.|
|`x`|Pornography.|
|`xf`|Unusual or extreme fetishes (e.g. genital torture, scat, vomit).|

## Trophy requirements