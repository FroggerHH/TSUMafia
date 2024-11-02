using TelegramBot.Utils;

namespace TelegramBot;

public enum Role : byte
{
    [Localized("️👨🏼 Мирный житель")] Normal = 0,
    [Localized("👨🏼‍⚕️ Доктор")] Doctor = 1,

    [Localized("🤵🏻 Мафиози")] Mafia = 128,

}