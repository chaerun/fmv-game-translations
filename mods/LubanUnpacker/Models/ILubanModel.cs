using Luban;

namespace LubanUnpacker.Models
{
    public interface ILubanModel
    {
        void Serialize(ByteBuf _buf);
    }
}
